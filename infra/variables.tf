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

variable "keepalived_password" {
  type      = string
  sensitive = true
}

########## Public ##########

variable "web_domain" {
  default = "web.walkablecity.app"
}

variable "api_domain" {
  default = "api.walkablecity.app"
}

variable "monitor_domain" {
  default = "monitor.walkablecity.app"
}

variable "region" {
  default = "fra1"
}

variable "minitwit_droplet_size" {
  default = "s-2vcpu-2gb"
}

variable "lb_droplet_size" {
  default = "s-1vcpu-1gb"
}

variable "db_droplet_size" {
  default = "s-1vcpu-1gb"
}

variable "reserved_ip" {
  default = "67.207.78.123"
  //default = "146.190.204.218" real ip
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