FROM debian:buster

ENV GS_SCENE="starting_zone"
ENV GS_PORT=50123

RUN apt update && \
    apt install -y wget unzip libxcursor-dev libxinerama-dev libxrandr-dev libxi-dev libgl-dev

COPY ./linux-server .
COPY ./server/entrypoint.sh entrypoint.sh

ENTRYPOINT [ "./entrypoint.sh" ]