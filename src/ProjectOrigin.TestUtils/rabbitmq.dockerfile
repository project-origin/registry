FROM rabbitmq:3.12

RUN rabbitmq-plugins enable --offline rabbitmq_management

EXPOSE 15672
