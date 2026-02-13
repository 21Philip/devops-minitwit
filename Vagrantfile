# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = 'digital_ocean'
  config.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
  config.ssh.private_key_path = '~/.ssh/id_rsa'

  ######################################### DB - API - WEBAPP #########################################

  config.vm.define "minitwit_server", primary: true do |server|
    server.vm.provider :digital_ocean do |provider|
      provider.ssh_key_name = ENV["SSH_KEY_NAME"]
      provider.token = ENV["DIGITAL_OCEAN_TOKEN"]
      provider.image = 'ubuntu-22-04-x64'
      provider.region = 'fra1'
      provider.size = 's-1vcpu-1gb'
    end

    server.vm.hostname = "minitwit_server"

    server.vm.provision "shell", inline: <<-SHELL
      # The following addresses an issue in DO's Ubuntu images, which still contain a lock file
      sudo fuser -vk -TERM /var/lib/apt/lists/lock
      sudo apt-get update

      # Install Docker
      curl -fsSL https://get.docker.com -o get-docker.sh
      sudo sh get-docker.sh

      # Allow running docker without sudo
      sudo usermod -aG docker $USER

      # Build and run
      cd /vagrant
      docker compose build
      nohup docker compose up

      THIS_IP=`hostname -I | cut -d" " -f1`
      echo "Api available at:"
      echo "http://${THIS_IP}:5000"
      echo "Webapp available at:"
      echo "http://${THIS_IP}:5001"
    SHELL
  end
end