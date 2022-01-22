#!bin/sh

if [ $GS_PCK -f ]; then 
    echo "main pack $GS_PCK does not exist" >&2
    exit 1;
fi

./Godot_v3.4.2-stable_mono_linux_server_64/Godot_v3.4.2-stable_mono_linux_server.64 --main-pack $GS_PCK --no-window $GS_SCENE --server $GS_PORT;