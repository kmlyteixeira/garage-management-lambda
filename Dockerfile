FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/GarageManagement.AuthLambda/GarageManagement.AuthLambda.csproj -c Release -o /app/publish

FROM public.ecr.aws/lambda/dotnet:8
COPY --from=build /app/publish ${LAMBDA_TASK_ROOT}
CMD [ "GarageManagement.AuthLambda::GarageManagement.AuthLambda.Function::FunctionHandler" ]
