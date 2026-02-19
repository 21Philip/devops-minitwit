terraform {
  required_providers {
    digitalocean = {
      source  = "digitalocean/digitalocean"
      version = "~> 2.0"
    }
  }
}

provider "digitalocean" {
  token = var.do_token
}

###################### Create droplet ######################

# Get ssh key defined on Digital Ocean website
data "digitalocean_ssh_key" "default" {
  name = "digital-ocean-ssh"
}

resource "digitalocean_droplet" "app" {
  name     = "minitwit"
  region   = var.region
  size     = var.droplet_size
  image    = "docker-20-04"
  ssh_keys = [data.digitalocean_ssh_key.default.id]

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = self.ipv4_address
  }

  provisioner "remote-exec" {
    inline = ["mkdir -p /app"]
  }

  # Copy docker-compose.yml to the droplet
  provisioner "file" {
    source      = "${path.module}/../docker-compose.yml"
    destination = "/app/docker-compose.yml"
  }

  # Pull images and start containers
  provisioner "remote-exec" {
    inline = [
      "cd /app",
      "docker compose pull",
      "docker compose up -d",
    ]
  }
}

###################### Assign reserved ip ######################

# Get reserved ip
data "digitalocean_reserved_ip" "app" {
  ip_address = var.reserved_ip
}

resource "digitalocean_reserved_ip_assignment" "app" {
  ip_address = data.digitalocean_reserved_ip.app.ip_address
  droplet_id = digitalocean_droplet.app.id
}

output "droplet_ip" {
  value = digitalocean_droplet.app.ipv4_address
}