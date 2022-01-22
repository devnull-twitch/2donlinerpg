FROM debian:buster

ENV GS_PCK="2DOnlienRPG.pck"
ENV GS_SCENE="overworld.tscn"
ENV GS_PORT=50123

RUN apt update && \
    apt install -y wget unzip libxcursor-dev libxinerama-dev libxrandr-dev libxi-dev libgl-dev && \
    wget https://downloads.tuxfamily.org/godotengine/3.4.2/mono/Godot_v3.4.2-stable_mono_linux_server_64.zip && \
    unzip Godot_v3.4.2-stable_mono_linux_server_64.zip && rm Godot_v3.4.2-stable_mono_linux_server_64.zip && \
    wget https://github.com/devnull-twitch/2donlinerpg/releases/latest/download/LinuxX11.zip && \
    unzip LinuxX11.zip && rm LinuxX11.zip

COPY ./server/entrypoint.sh entrypoint.sh

ENTRYPOINT [ "./entrypoint.sh" ]