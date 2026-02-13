# -*- mode: ruby -*-
# vi: set ft=ruby :

$ip_file = "deployment_ip.txt"

Vagrant.configure("2") do |config|
  config.vm.box = 'digital_ocean'
  config.vm.box_url = "https://github.com/devopsgroup-io/vagrant-digitalocean/raw/master/box/digital_ocean.box"
  config.ssh.private_key_path = '~/.ssh/id_rsa'
  config.vm.synced_folder ".", "/vagrant", type: "rsync"

  # Single webserver for ITU-MiniTwit
  config.vm.define "itu-minitwit", primary: true do |server|
    server.vm.provider :digital_ocean do |provider|
      provider.ssh_key_name = "itu-minitwit-key"  
      provider.token = ENV["DIGITALOCEAN_TOKEN"]   
      provider.image = 'ubuntu-22-04-x64'
      provider.region = 'fra1'  # Frankfurt
      provider.size = 's-1vcpu-1gb'  # $4/month droplet
      provider.privatenetworking = true
    end

    server.vm.hostname = "itu-minitwit"

    server.trigger.after :up do |trigger|
      trigger.info = "Writing droplet IP to file..."
      trigger.ruby do |env,machine|
        remote_ip = machine.instance_variable_get(:@communicator).instance_variable_get(:@connection_ssh_info)[:host]
        File.write($ip_file, remote_ip)
        puts "Droplet IP: #{remote_ip}"
      end
    end

    server.vm.provision "shell", inline: <<-SHELL
      # Fix APT lock issue (common on DigitalOcean)
      sudo fuser -vk -TERM /var/lib/apt/lists/lock
      sudo apt-get update
      # Skip upgrade - not needed, saves 10+ minutes

      # Install Docker
      sudo apt-get install -y \
        ca-certificates \
        curl \
        gnupg \
        lsb-release

      sudo mkdir -p /etc/apt/keyrings
      curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
      
      echo \
        "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
        $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

      sudo apt-get update
      sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

      # Enable Docker daemon
      sudo systemctl enable docker
      sudo systemctl start docker

      # Add vagrant user to docker group (optional, for non-root docker commands)
      sudo usermod -aG docker vagrant

      # Create app directory
      sudo mkdir -p /opt/itu-minitwit
      sudo chown -R vagrant:vagrant /opt/itu-minitwit

      # Copy application files
      cp -r /vagrant/* /opt/itu-minitwit/

      # Build and run Docker container
      cd /opt/itu-minitwit
      sudo docker build -t itu-minitwit:latest .
      sudo docker run -d \
        --name itu-minitwit \
        --restart unless-stopped \
        -p 8080:8080 \
        itu-minitwit:latest

      echo "================================================================="
      echo "=              ITU-MiniTwit Deployment Complete                 ="
      echo "================================================================="
      THIS_IP=`hostname -I | cut -d" " -f1`
      echo "Application URL: http://${THIS_IP}:8080"
      echo "================================================================="
    SHELL
  end
end
