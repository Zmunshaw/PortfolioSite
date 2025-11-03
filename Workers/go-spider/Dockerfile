# Build stage
FROM golang:latest AS build
WORKDIR /app

COPY go.mod .
COPY go.sum .
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o app .

# Final stage
FROM alpine:latest AS run
RUN apk --no-cache add ca-certificates
WORKDIR /app

# Copy only the binary from the build stage
COPY --from=build /app/app .

EXPOSE 9900
CMD ["./app"]