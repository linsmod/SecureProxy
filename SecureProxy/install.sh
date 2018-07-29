
#!/bin/bash
install_path="/root/spsvr";
start_file=${install_path}"/spsvr.sh"
if [ ! -d $install_path ]; then
  sudo mkdir -p -m 755 $install_path
fi
cd $install_path
if [ ! -f "publish_linux_x64.zip" ]; then
  wget -N https://github.com/kissstudio/SecureProxy/releases/download/v1.1/indp_linux_x64.zip
fi

apt-get -y install unzip
unzip -o publish_linux_x64.zip
chmod -R 755 ${install_path}/"SecureProxyServer"
apt-get -y install  libunwind8-dev
if [ ! -f "/etc/init.d/spsvr" ]; then
    #ln -s ${start_file} /etc/init.d/spsvr
    cp ${start_file} /etc/init.d/spsvr
else
    rm /etc/init.d/spsvr
    #ln -s ${start_file} /etc/init.d/spsvr
    cp ${start_file} /etc/init.d/spsvr
fi

chmod -R 755 /etc/init.d/spsvr
/etc/init.d/spsvr start
update-rc.d spsvr defaults 98
