# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = 'digital_ocean'
  config.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
  config.ssh.private_key_path = '~/.ssh/id_rsa'
  config.vm.synced_folder ".", "/vagrant", type: "rsync"

  ######################################### DB - API - WEBAPP #########################################

  config.vm.define "minitwit", primary: true do |server|
    server.vm.provider :digital_ocean do |provider|
      provider.ssh_key_name = ENV["SSH_KEY_NAME"]
      provider.token = ENV["DIGITAL_OCEAN_TOKEN"]
      provider.image = 'ubuntu-22-04-x64'
      provider.region = 'fra1'
      provider.size = 's-1vcpu-1gb'
    end

    server.vm.hostname = "minitwit"

    server.vm.provision "shell", inline: <<-SHELL
      # The following addresses an issue in DO's Ubuntu images, which still contain a lock file
      while sudo fuser /var/lib/dpkg/lock-frontend >/dev/null 2>&1; do
        echo "Waiting for apt lock..."
        sleep 5
      done

      while sudo fuser /var/lib/apt/lists/lock >/dev/null 2>&1; do
        echo "Waiting for apt lists lock..."
        sleep 5
      done

      sudo apt-get update

      # Install Docker
      curl -fsSL https://get.docker.com -o get-docker.sh
      sudo sh get-docker.sh

      # Allow running docker without sudo
      sudo usermod -aG docker vagrant

      # Build and run
      cd /vagrant
      sudo docker compose build
      nohup sudo docker compose up

      THIS_IP=`hostname -I | cut -d" " -f1`
      echo "Api available at:"
      echo "http://${THIS_IP}:5000"
      echo "Webapp available at:"
      echo "http://${THIS_IP}:5001"
    SHELL
  end
end