#!/bin/bash

echo "Waiting for Redpanda..."

until rpk cluster info --brokers redpanda:9092 > /dev/null 2>&1; do
  sleep 2
done

echo "Creating topics..."

topics=(
  "aanvragers"
  "comedy-movies"
  "drama-movies"
  "fantasy-movies"
  "game-commands-development"
  "game-go-development"
  "game-help-development"
  "game-locations-development"
  "game-look-development"
  "game-player-location-development"
  "game-player-position-development"
  "game-response-development"
  "high-risk-orders"
  "horror-movies"
  "low-risk-orders"
  "medium-risk-orders"
  "movies"
  "my-topic"
  "orders"
  "other-topic"
  "payments"
  "payed-orders"
  "retry"
  "thriller-movies"
  "uitgangspunten"
  "uitgangspunten-met-aanvragers"
  "unknown-movies"
  "upload-file"
)

for topic in "${topics[@]}"; do
  rpk topic create "$topic" --brokers redpanda:9092 || true
done

echo "Done"