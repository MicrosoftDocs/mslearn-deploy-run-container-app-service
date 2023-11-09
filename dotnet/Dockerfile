FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM base AS final
WORKDIR /app
COPY src .
COPY entrypoint.sh .
RUN apt-get update \
    && apt-get install -y --no-install-recommends dialog \
    && apt-get install -y --no-install-recommends openssh-server \
    && echo "root:Docker!" | chpasswd \
    && chmod u+x /app/entrypoint.sh

COPY sshd_config /etc/ssh/
EXPOSE 8000 2222
ENTRYPOINT [ "/app/entrypoint.sh" ]