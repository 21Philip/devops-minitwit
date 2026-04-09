########## Private ##########

variable "do_token" {
  description = "DigitalOcean API token"
  sensitive   = true
}

variable "ssh_private_key" {
  description = "Private key for droplet ssh authentication"
  sensitive   = true
}

variable "postgres_user" {
  type      = string
  sensitive = true
}

variable "postgres_password" {
  type      = string
  sensitive = true
}

variable "postgres_db" {
  type      = string
  sensitive = true
}

variable "db_connection" {
  type      = string
  sensitive = true
}

########## Public ##########

variable "region" {
  default = "fra1"
}

variable "minitwit_droplet_size" {
  default = "s-2vcpu-4gb"
}

variable "lb_droplet_size" {
  default = "s-1vcpu-1gb"
}

variable "db_droplet_size" {
  default = "s-1vcpu-1gb"
}

variable "reserved_ip" {
  default = "146.190.204.218"
}

variable "volume_name" {
  default = "volume-fra1-01"
}

variable "minitwit_instance_names" {
  type = list(string)
  default = ["minitwit1", "minitwit2"]
}

variable "load_balancer_names" {
  type = list(string)
  default = ["lb1", "lb2"]
}