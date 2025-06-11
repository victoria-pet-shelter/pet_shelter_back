PROJECT = ../backend

run:
	@dotnet run --project $(PROJECT)

build:
	dotnet build $(PROJECT)

clean:
	@rd /s /q $(PROJECT)\bin && rd /s /q $(PROJECT)\obj || echo Clean complete
	@dotnet restore $(PROJECT)

migrate:
	dotnet ef database update --project $(PROJECT)

resetdb:
	@echo Dropping and recreating database...
	dropdb pet_shelters --if-exists
	createdb pet_shelters
	dotnet ef database update --project $(PROJECT)

cleandocker:
	@echo Cleaning up all Docker data...
	@docker system prune -a --volumes -f
