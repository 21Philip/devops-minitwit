# -*- mode: ruby -*-
# vi: set ft=ruby :

$DB_USER = "chirpuser"
$DB_PASS = "chirppass"
$DB_NAME = "chirpdb"

Vagrant.configure("2") do |config|
  config.vm.box = 'digital_ocean'
  config.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
  config.ssh.private_key_path = '~/.ssh/id_rsa'

  ######################################### DATABASE #########################################

  config.vm.define "dbserver", primary: true do |server|
    server.vm.provider :digital_ocean do |provider|
      provider.ssh_key_name = ENV["SSH_KEY_NAME"]
      provider.token = ENV["DIGITAL_OCEAN_TOKEN"]
      provider.image = 'ubuntu-22-04-x64'
      provider.region = 'fra1'
      provider.size = 's-1vcpu-1gb'
      provider.privatenetworking = true
    end

    server.vm.hostname = "dbserver"

    server.vm.provision "shell", inline: <<-SHELL
      # The following addresses an issue in DO's Ubuntu images, which still contain a lock file
      sudo fuser -vk -TERM /var/lib/apt/lists/lock
      sudo apt-get update

      # Install Docker
      curl -fsSL https://get.docker.com -o get-docker.sh
      sudo sh get-docker.sh

      # Allow running docker without sudo
      sudo usermod -aG docker $USER

      # Start PostgreSQL
      docker run -d --name postgresdb \
        -e POSTGRES_USER=$DB_USER \
        -e POSTGRES_PASSWORD=$DB_PASS \
        -e POSTGRES_DB=$DB_NAME \
        -p 5432:5432 \
        postgres:15
    SHELL
  end

  ######################################### API #########################################

  config.vm.define "apiserver", primary: true do |server|
    server.vm.provider :digital_ocean do |provider|
      provider.ssh_key_name = ENV["SSH_KEY_NAME"]
      provider.token = ENV["DIGITAL_OCEAN_TOKEN"]
      provider.image = 'ubuntu-22-04-x64'
      provider.region = 'fra1'
      provider.size = 's-1vcpu-1gb'
      provider.privatenetworking = true
    end

    server.vm.hostname = "apiserver"

    server.vm.provision "shell", inline: <<-SHELL
      # The following addresses an issue in DO's Ubuntu images, which still contain a lock file
      sudo fuser -vk -TERM /var/lib/apt/lists/lock
      sudo apt-get update

      # Install Docker
      curl -fsSL https://get.docker.com -o get-docker.sh
      sudo sh get-docker.sh

      # Allow running docker without sudo
      sudo usermod -aG docker $USER

      # Get DB private IP
      DB_IP=$(getent hosts dbserver | awk '{ print $1 }')

      # Build and run image
      cp -r /src $HOME
      docker build -t apiserver -f src/Chirp.Api/Dockerfile .
      docker run -d -p 5000:5000 \
        -e DB_HOST=$DB_IP \
        -e POSTGRES_USER=$DB_USER \
        -e POSTGRES_PASSWORD=$DB_PASS \
        -e POSTGRES_DB=$DB_NAME \
        apiserver:latest

      echo "Api available at:"
      THIS_IP=`hostname -I | cut -d" " -f1`
      echo "http://${THIS_IP}:5000"
    SHELL
  end

  ######################################### WEBSITE #########################################

  config.vm.define "webserver", primary: false do |server|
    server.vm.provider :digital_ocean do |provider|
      provider.ssh_key_name = ENV["SSH_KEY_NAME"]
      provider.token = ENV["DIGITAL_OCEAN_TOKEN"]
      provider.image = 'ubuntu-22-04-x64'
      provider.region = 'fra1'
      provider.size = 's-1vcpu-1gb'
      provider.privatenetworking = true
    end

    server.vm.hostname = "webserver"

    server.vm.provision "shell", inline: <<-SHELL
      # The following addresses an issue in DO's Ubuntu images, which still contain a lock file
      sudo fuser -vk -TERM /var/lib/apt/lists/lock
      sudo apt-get update

      # Install Docker
      curl -fsSL https://get.docker.com -o get-docker.sh
      sudo sh get-docker.sh

      # Allow running docker without sudo
      sudo usermod -aG docker $USER

      # Get DB private IP
      DB_IP=$(getent hosts dbserver | awk '{ print $1 }')

      # Build and run image
      cp -r /src $HOME
      docker build -t webserver -f src/Chirp.Web/Dockerfile .

      docker run -d -p 5000:5000 \
        -e DB_HOST=$DB_IP \
        -e POSTGRES_USER=$DB_USER \
        -e POSTGRES_PASSWORD=$DB_PASS \
        -e POSTGRES_DB=$DB_NAME \
        webserver:latest

      echo "Website available at:"
      THIS_IP=`hostname -I | cut -d" " -f1`
      echo "http://${THIS_IP}:5000"
    SHELL
  end
end