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

# Get default Digital Ocean ssh key
data "digitalocean_ssh_key" "default" {
  name = "digital-ocean-ssh"
}

# Get personal ssh keys (add your own)
data "digitalocean_ssh_key" "philip" {
  name = "philip-hjem"
}

###################### Create database ######################

resource "digitalocean_droplet" "database" {
  name     = "database1"
  region   = var.region
  size     = var.db_droplet_size
  image    = "docker-20-04"
  ssh_keys = [data.digitalocean_ssh_key.default.id, data.digitalocean_ssh_key.philip.id]

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
  count    = length(var.minitwit_instance_names)
  name     = var.minitwit_instance_names[count.index]
  region   = var.region
  size     = var.minitwit_droplet_size
  image    = "docker-20-04"
  ssh_keys = [data.digitalocean_ssh_key.default.id, data.digitalocean_ssh_key.philip.id]

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
  ssh_keys = [data.digitalocean_ssh_key.default.id, data.digitalocean_ssh_key.philip.id]

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = self.ipv4_address
  }

  ########### Nginx ##########
  provisioner "remote-exec" {
    inline = [
      "apt-get update",
      "apt-get install -y nginx",
      "ufw allow 'Nginx Full'",
    ]
  }

  provisioner "file" {
    content     = templatefile("${path.module}/assets/nginx.conf.tftpl", {
      backend_ips    = digitalocean_droplet.minitwit[*].ipv4_address_private
      web_domain     = var.web_domain
      api_domain     = var.api_domain
      monitor_domain = var.monitor_domain
    })
    destination = "/etc/nginx/sites-available/default"
  }

  ########## Keepalived ##########
  provisioner "remote-exec" {
    inline = [
      "apt-get install -y keepalived",
      "mkdir -p /etc/keepalived",
      "cd /etc/keepalived",
      "curl -LO http://do.co/assign-ip",
    ]
  }

  # Initial configuration of keepalived
  provisioner "file" {
    source      = "${path.module}/assets/keepalived_init"
    destination = "/etc/init/keepalived.conf"
  }

  provisioner "file" {
    content     = templatefile("${path.module}/assets/master.sh.tftpl", {
      do_token    = var.do_token
      reserved_ip = var.reserved_ip
    })
    destination = "/etc/keepalived/master.sh"
  }

  provisioner "remote-exec" {
    inline = ["chmod +x /etc/keepalived/master.sh"]
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

// Finish keepalived configuration and start keepalived
resource "null_resource" "start_keepalived" {
  depends_on = [digitalocean_droplet.load_balancers, digitalocean_reserved_ip_assignment.minitwit]
  count = length(digitalocean_droplet.load_balancers)

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = digitalocean_droplet.load_balancers[count.index].ipv4_address
  }

  provisioner "file" {
    content     = templatefile("${path.module}/assets/keepalived.conf.tftpl", {
      state    = count.index == 0 ? "MASTER" : "BACKUP"
      priority = 250 - (count.index * 5)
      self_ip  = digitalocean_droplet.load_balancers[count.index].ipv4_address_private
      peer_ips = [for i, ip in digitalocean_droplet.load_balancers[*].ipv4_address_private :
                  ip if i != count.index]
      password = var.keepalived_password
    })
    destination = "/etc/keepalived/keepalived.conf"
  }

  provisioner "remote-exec" {
    inline = [
      "systemctl enable keepalived",
      "systemctl start keepalived",
    ]
  }
}

// Finish nginx configuration abd start nginx
resource "null_resource" "create_certificates" {
  depends_on = [digitalocean.load_balancers]

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = digitalocean_droplet.load_balancers[0].ipv4_address
  }

  provisioner "remote-exec" {
    inline = [
      "curl https://get.acme.sh | sh -s email=bruh@mail.com",
      "export Namecom_Username=${var.namecom_username}",
      "export Namecom_Token=${var.namecom_token}",
      "~/.acme.sh/acme.sh --issue --dns dns_namecom -d 'walkablecity.app' -d '*.walkablecity.app'",
      "mkdir -p /etc/letsencrypt/live/walkablecity.app",
      "~/.acme.sh/acme.sh --install-cert -d 'walkablecity.app' \
        --cert-file /etc/letsencrypt/live/walkablecity.app/cert.pem \
        --key-file /etc/letsencrypt/live/walkablecity.app/privkey.pem \
        --fullchain-file /etc/letsencrypt/live/walkablecity.app/fullchain.pem",
      "systemctl enable nginx",
      "systemctl start nginx",
    ]
  }
}