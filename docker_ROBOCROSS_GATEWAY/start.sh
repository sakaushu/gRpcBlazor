#!/bin/bash

if [ "$1" = "debug" ]; then
    DEBUG=true docker compose up -d --build
else
    docker compose up -d --build
fi
