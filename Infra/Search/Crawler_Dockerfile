# Build stage
FROM golang:latest AS build
WORKDIR /app

COPY go.mod .
COPY go.sum .
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o app .

# Final stage
FROM python:3.11-alpine AS run
RUN apk --no-cache add ca-certificates gcc musl-dev libxml2-dev libxslt-dev
WORKDIR /app

COPY --from=build /app/app .
COPY --from=build /app/python ./python

# Create virtual environment and install requirements
RUN python3 -m venv ./venv && \
    ./venv/bin/pip install --upgrade pip && \
    ./venv/bin/pip install -r python/requirements.txt

EXPOSE 9900
CMD ["./app"]