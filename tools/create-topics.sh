#!/bin/bash

echo "Waiting for Redpanda..."

until rpk cluster info --brokers redpanda:9092 > /dev/null 2>&1; do
  sleep 2
done

echo "Creating topics..."

topics=(
  "aanvragers"
  "drama-movies"
  "fantasy-movies"
  "game-commands-development"
  "game-echo-development"
  "game-go-development"
  "game-help-development"
  "game-inventory-development"
  "game-locations-development"
  "game-look-development"
  "game-player-location-development"
  "game-player-position-development"
  "game-response-development"
  "horror-movies"
  "left"
  "left-update"
  "locations"
  "movies"
  "my-topic"
  "orders"
  "other-topic"
  "right"
  "topic-1"
  "topic-2"
  "topic-3"
  "topic-a"
  "topic-b"
  "topic-c"
  "topic.name"
  "uitgangspunten"
  "uitgangspunten-met-aanvragers"
  "unknown"
  "unknown-movies"
)

for topic in "${topics[@]}"; do
  rpk topic create "$topic" --brokers redpanda:9092 || true
done

echo "Done"