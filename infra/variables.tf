variable "do_token" {
  description = "DigitalOcean API token"
  sensitive   = true
}

variable "ssh_private_key" {
  description = "Private key for droplet ssh authentication"
  sensitive   = true
}

variable "region" {
  default = "fra1"
}

variable "droplet_size" {
  default = "s-1vcpu-1gb"
}