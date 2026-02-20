#!/bin/bash

# autosetup.sh
# ROBOCROSS-Gateway 自動セットアップスクリプト
# このスクリプトは Root 権限で実行してください。

# $workdir に転送された zip ファイルを展開し、
# $app_home にアプリケーションをセットアップします。

# /opt/srcにプロジェクトのフォルダをそのままZIP化したものを置いて実行してください。
# 例: /opt/src/ROBOCROSS-Gateway.zip
export app_home=/opt/roboxgw
export workdir=/opt/src
export debian_user=smoriza

export debian_ip=$(ip -4 addr show eth0 | awk '/inet /{print $2}' | cut -d/ -f1)
echo "eth0 IP = $debian_ip"
export zip_name=ROBOCROSS-Gateway
export zip_home=$workdir/$zip_name


# ------------------------------
# SCPなどで通常ユーザーのホームディレクトリなどに転送したzipファイルを
# ワークディレクトリにコピーします。
# 手動でコピーする場合はコメントアウトしてください。
# ------------------------------

# # 転送されてきたファイルを取得
# cd $workdir
# # 古いファイルを削除
# rm -rf $zip_name
# rm $zip_name.zip
# # 転送された zip ファイルをコピー
# cp /home/$debian_user/$zip_name.zip ./

# ------------------------------
# zipファイルコピーここまで
# ------------------------------

# アプリ停止
cd $app_home
docker compose down

# 作業ディレクトリへ移動
cd $workdir

# アプリのインストール先をクリーンアップ
rm -rf $app_home

# ディレクトリ作成
mkdir -p $app_home
mkdir -p $app_home/docker
mkdir -p $app_home/launcher
mkdir -p $app_home/core
mkdir -p $app_home/mnt
mkdir -p $app_home/mnt/settings
mkdir -p $app_home/src
mkdir -p $app_home/scripts

# ---------------------------------------------------- #
# 各種設定ファイルをマウントするディレクトリにコピーして
# 元の場所にシンボリックリンクを作成します。
# ---------------------------------------------------- #

# バックアップ用ディレクトリ
export backup_dir=$workdir/backup
mkdir -p $backup_dir

# debian設定ファイルのバックアップ先
export debian_conf_backup_dir=$backup_dir/mnt/settings/debian
mkdir -p $debian_conf_backup_dir

# 設定ファイルを配置するディレクトリ
export conf_dir=$app_home/mnt/settings
mkdir -p $conf_dir/debian

# 2-2-2 時間設定
# ---------------
# /etc/systemd/timesyncd.confがシンボリックリンクならバックアップファイルで上書きする
if [ -L /etc/systemd/timesyncd.conf ]; then
    echo "timesyncd.conf is a symbolic link. Restoring from backup."
    rm /etc/systemd/timesyncd.conf
    cp $debian_conf_backup_dir/timesyncd.conf /etc/systemd/timesyncd.conf
fi
cp /etc/systemd/timesyncd.conf $conf_dir/debian/timesyncd.conf
# 元の設定ファイルを削除
rm /etc/systemd/timesyncd.conf
# シンボリックリンクを作成
ln -s $conf_dir/debian/timesyncd.conf /etc/systemd/timesyncd.conf
# バックアップ
cp $conf_dir/debian/timesyncd.conf $debian_conf_backup_dir/timesyncd.conf

# ---------------------------------------------------- #
# 設定ファイルのコピーここまで
# ---------------------------------------------------- #


# セットアップ
unzip $zip_name.zip
cp -r $zip_home/tools/docker/docker_ROBOCROSS_GATEWAY/* $app_home/
#cp -r $zip_home/tools/scripts/* $app_home/scripts/
#rm -rf $app_home/src/GATEWAY_Core
#rm -rf $app_home/src/GATEWAY_Launcher
cp -r $zip_home/GATEWAY_Core $app_home/src/
cp -r $zip_home/GATEWAY_Launcher $app_home/src/

# サービスの配置
cp $zip_home/tools/host_files/services/saferun@.service /etc/systemd/system/
systemctl daemon-reload

# サービス起動用スクリプトの配置
cp -r $zip_home/tools/host_files/scripts/* $app_home/scripts/

cd $app_home
docker compose build

docker compose up -d

# 不要なイメージを削除
docker image prune -f

