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
      # Disable automatic apt services during provisioning
      sudo systemctl stop apt-daily.service
      sudo systemctl stop apt-daily-upgrade.service
      sudo systemctl kill --kill-who=all apt-daily.service
      sudo systemctl kill --kill-who=all apt-daily-upgrade.service
      sudo systemctl disable apt-daily.service
      sudo systemctl disable apt-daily-upgrade.service

      # Wait until dpkg lock is released
      while sudo fuser /var/lib/dpkg/lock-frontend >/dev/null 2>&1; do
        echo "Waiting for dpkg lock..."
        sleep 3
      done

      # Install Docker
      sudo apt-get update
      sudo apt-get install -y ca-certificates curl gnupg lsb-release

      sudo mkdir -p /etc/apt/keyrings
      curl -fsSL https://download.docker.com/linux/ubuntu/gpg | \
        sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg

      echo \
        "deb [arch=$(dpkg --print-architecture) \
        signed-by=/etc/apt/keyrings/docker.gpg] \
        https://download.docker.com/linux/ubuntu \
        $(lsb_release -cs) stable" | \
        sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

      sudo apt-get update
      sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

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