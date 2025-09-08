FROM grafana/k6:latest

WORKDIR /scripts

COPY . .

ENTRYPOINT ["k6"]
CMD ["run", "/scripts/scenarios/cashiers/baseline.js"]