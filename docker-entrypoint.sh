#!/bin/bash
set -e

# Change owner for our uploads folder
echo -n "Fix permissions for mounted volumes ..." && \
  chown -R abc:abc $Storage__UploadDirectory && \
  chown -R abc:abc /etc/ssl/private && \
  chown -R abc:abc /app && \
  echo "done!"


echo "
--------------------------------------------------------------------------
http proxy:                       ${PROXY:-no proxy set}
http proxy exceptions:            $([[ -n $NO_PROXY ]] && echo $NO_PROXY || echo undefined)
user uid:                         $(id -u abc)
user gid:                         $(id -g abc)
timezone:                         $TZ
--------------------------------------------------------------------------
"

echo -e "geocop app is up and running!\n" && \
  sudo -H --preserve-env --user abc dotnet GeoCop.Api.dll
