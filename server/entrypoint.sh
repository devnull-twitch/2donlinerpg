#!bin/sh

if [ $GS_PCK -f ]; then 
    echo "main pack $GS_PCK does not exist" >&2
    exit 1;
fi

./2DOnlienRPG.x86_64 scenes/Game.tscn --world $GS_SCENE --server $GS_PORT;