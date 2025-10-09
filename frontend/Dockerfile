FROM node:14 AS build

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm install

COPY . .

RUN npm run build --prod

FROM nginx:alpine

COPY --from=build /app/dist/sentiment-analyzer-app /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]