# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = 'digital_ocean'
  config.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
  config.ssh.private_key_path = '~/.ssh/id_rsa'
  config.vm.synced_folder "./docker-compose.yml", "/vagrant", type: "rsync"

  #########################################
  # DB - API - WEBAPP            (currently all deployed to same server)
  #########################################
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
      # Wait for cloud-init and any initial apt processes
      echo "Waiting for system initialization..."
      sudo cloud-init status --wait || true
      
      # Wait for locks
      while sudo fuser /var/lib/dpkg/lock-frontend >/dev/null 2>&1 || \
            sudo fuser /var/lib/dpkg/lock >/dev/null 2>&1 || \
            sudo fuser /var/lib/apt/lists/lock >/dev/null 2>&1; do
        sleep 3
      done
      
      sleep 5  # Extra buffer

      # Install Docker
      echo "Installing Docker..."
      curl -fsSL https://get.docker.com -o get-docker.sh
      sudo sh get-docker.sh
      rm get-docker.sh

      sudo usermod -aG docker vagrant # TODO: vagrant user aparently dont exist??
      sudo systemctl enable docker
      sudo systemctl start docker
      sleep 3

      cd /vagrant
      echo "Pulling and running containers"
      sudo docker compose pull
      sudo docker compose up -d

      THIS_IP=$(hostname -I | cut -d" " -f1)
      echo "========================================="
      echo "Deployment complete!"
      echo "Api available at: http://${THIS_IP}:5000"
      echo "Webapp available at: http://${THIS_IP}:5001"
      echo "========================================="
    SHELL
  end
end