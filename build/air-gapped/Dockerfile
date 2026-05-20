# syntax=docker/dockerfile:1

FROM cgr.dev/chainguard/nginx:latest
COPY --from=config nginx.conf /etc/nginx/nginx.conf
COPY --from=content . /usr/share/nginx/html/
EXPOSE 8080
