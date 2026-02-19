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

# Get ssh key defined on Digital Ocean website
data "digitalocean_ssh_key" "default" {
  name = "digital-ocean-ssh"
}

resource "digitalocean_droplet" "app" {
  name     = "app-server"
  region   = var.region
  size     = var.droplet_size
  image    = "docker-20-04"
  ssh_keys = [data.digitalocean_ssh_key.default.id]

  # cloud-init script: runs once on first boot
  user_data = <<-EOF
    #!/bin/bash
    mkdir -p /app
  EOF

  connection {
    type        = "ssh"
    user        = "root"
    private_key = var.ssh_private_key
    host        = self.ipv4_address
  }

  # Copy docker-compose.yml to the droplet
  provisioner "file" {
    source      = "${path.module}/docker-compose.yml"
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

output "droplet_ip" {
  value = digitalocean_droplet.app.ipv4_address
}