FROM node:current-alpine
COPY /node/. /app
WORKDIR /app
RUN npm install
ENTRYPOINT ["npm", "start"]