FROM debian:buster

ENV GS_PCK="2DOnlienRPG.pck"
ENV GS_SCENE="starting_zone"
ENV GS_PORT=50123

RUN apt update && \
    apt install -y wget unzip libxcursor-dev libxinerama-dev libxrandr-dev libxi-dev libgl-dev && \
    wget https://github.com/devnull-twitch/2donlinerpg/releases/latest/download/LinuxServer.zip && \
    unzip LinuxServer.zip && rm LinuxServer.zip

COPY ./server/entrypoint.sh entrypoint.sh

ENTRYPOINT [ "./entrypoint.sh" ]