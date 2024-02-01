FROM rabbitmq:3.11

RUN rabbitmq-plugins enable --offline rabbitmq_management

EXPOSE 15672
