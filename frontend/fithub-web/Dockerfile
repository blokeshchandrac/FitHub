FROM node:22.22.3-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.29-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/fithub-web/browser /usr/share/nginx/html
EXPOSE 80
