#!/bin/bash

echo "Waiting for Redpanda..."

until rpk cluster info --brokers redpanda:9092 > /dev/null 2>&1; do
  sleep 2
done

echo "Creating topics..."

topics=(
  "aanvragers"
  "Comedy-movies"
  "drama-movies"
  "fantasy-movies"
  "high-risk-orders"
  "Horror-movies"
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
  "Thriller-movies"
  "uitgangspunten"
  "uitgangspunten-met-aanvragers"
  "unknown-movies"
  "upload-file"
)

for topic in "${topics[@]}"; do
  rpk topic create "$topic" --brokers redpanda:9092 || true
done

echo "Done"