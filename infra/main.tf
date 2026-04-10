terraform {
  required_providers {
    digitalocean = {
      source  = "digitalocean/digitalocean"
      version = "~> 2.0"
    }
    null = {
      source  = "hashicorp/null"
      version = "~> 3.0"
    }
  }
}

provider "digitalocean" {
  token = var.do_token
}

# Get ssh key defined on Digital Ocean website
data "digitalocean_ssh_key" "default" {
  name = "digital-ocean-ssh"
}

###################### Create database ######################

resource "digitalocean_droplet" "database" {
  name               = "database1"
  region             = var.region
  size               = var.db_droplet_size
  image              = "docker-20-04"
  ssh_keys           = [data.digitalocean_ssh_key.default.id]

  // Create the /app directory but do not start containers
  // until volume has been attached.
  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = self.ipv4_address
  }

  provisioner "remote-exec" {
    inline = ["mkdir -p /app"]
  }

  provisioner "file" {
    content     = templatefile("${path.module}/assets/.env.db.tftpl", {
      postgres_user     = var.postgres_user
      postgres_password = var.postgres_password
      postgres_db       = var.postgres_db
      volume_name       = var.volume_name
    })
    destination = "/app/.env"
  }

  provisioner "file" {
    source      = "${path.module}/../docker-compose-db.yml"
    destination = "/app/docker-compose-db.yml"
  }
}

// Get existing volume and attach to droplet
data "digitalocean_volume" "database" {
  name = var.volume_name
}

resource "digitalocean_volume_attachment" "database" {
  droplet_id = digitalocean_droplet.database.id
  volume_id  = data.digitalocean_volume.database.id
}

// Start the containers
resource "null_resource" "start_database" {
  depends_on = [digitalocean_volume_attachment.database]

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = digitalocean_droplet.database.ipv4_address
  }

  provisioner "remote-exec" {
    inline = [
      "cd /app",
      "docker compose -f docker-compose-db.yml pull",
      "docker compose -f docker-compose-db.yml up -d",
    ]
  }
}

###################### Create Minitwit instances ######################

resource "digitalocean_droplet" "minitwit" {
  count              = length(var.minitwit_instance_names)
  name               = var.minitwit_instance_names[count.index]
  region             = var.region
  size               = var.minitwit_droplet_size
  image              = "docker-20-04"
  ssh_keys           = [data.digitalocean_ssh_key.default.id]

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = self.ipv4_address
  }

  provisioner "remote-exec" {
    inline = ["mkdir -p /app"]
  }

  provisioner "file" {
    content     = templatefile("${path.module}/assets/.env.app.tftpl", {
      postgres_host     = digitalocean_droplet.database.ipv4_address_private
      postgres_user     = var.postgres_user
      postgres_password = var.postgres_password
      postgres_db       = var.postgres_db
    })
    destination = "/app/.env"
  } 

  # Copy docker-compose.yml to the droplet
  provisioner "file" {
    source      = "${path.module}/../docker-compose-app.yml"
    destination = "/app/docker-compose-app.yml"
  }

  # Pull images and start containers
  provisioner "remote-exec" {
    inline = [
      "cd /app",
      "docker compose -f docker-compose-app.yml pull",
      "docker compose -f docker-compose-app.yml up -d",
    ]
  } 
}

###################### Create load balancers ######################

resource "digitalocean_droplet" "load_balancers" {
  count    = length(var.load_balancer_names)
  name     = var.load_balancer_names[count.index]
  region   = var.region
  size     = var.lb_droplet_size
  image    = "docker-20-04"
  ssh_keys = [data.digitalocean_ssh_key.default.id]

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = self.ipv4_address
  }

  provisioner "remote-exec" {
    inline = [
      "apt-get update",
      "apt-get install -y nginx keepalived",
    ]
  }

  # Configure Nginx
  provisioner "file" {
    content     = templatefile("${path.module}/assets/nginx.conf.tftpl", {
      backend_ips = digitalocean_droplet.minitwit[*].ipv4_address_private
    })
    destination = "/etc/nginx/sites-available/default"
  }
  
  provisioner "remote-exec" {
    inline = [
      "nginx -t",
      "systemctl restart nginx",
      "systemctl enable nginx",
      "ufw allow 80/tcp"
    ]
  }

  # Create Nginx health check script
  provisioner "file" {
    source      = "${path.module}/assets/check_nginx.sh"
    destination = "/etc/keepalived/check_nginx.sh"
  }

  provisioner "remote-exec" {
    inline = ["chmod +x /etc/keepalived/check_nginx.sh"]
  }

  # Configure Keepalived
  provisioner "file" {
    content     = templatefile("${path.module}/assets/keepalived.conf.tftpl", {
      state       = count.index == 0 ? "MASTER" : "BACKUP"
      priority    = 255 - (count.index * 5)
      reserved_ip = var.reserved_ip
      password    = var.keepalived_password
    })
    destination = "/etc/keepalived/keepalived.conf"
  }

  provisioner "remote-exec" {
    inline = [
      "echo 'net.ipv4.ip_nonlocal_bind=1' >> /etc/sysctl.conf",
      "sysctl -p",
      "systemctl start keepalived",
      "systemctl enable keepalived",
    ]
  }
}

// Get and assign reserved ip
data "digitalocean_reserved_ip" "minitwit" {
  ip_address = var.reserved_ip
}

resource "digitalocean_reserved_ip_assignment" "minitwit" {
  ip_address = data.digitalocean_reserved_ip.minitwit.ip_address
  droplet_id = digitalocean_droplet.load_balancers[0].id
}
