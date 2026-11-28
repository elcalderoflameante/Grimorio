# Kitchen Recipe Checks

Integration checks use only the isolated PostgreSQL database on
`127.0.0.1:15441`, database `kitchen_recipe_checks`, user `checks`.

```powershell
docker run --detach --rm --name grimorio_kitchen_recipe_checks --publish 127.0.0.1:15441:5432 --env POSTGRES_DB=kitchen_recipe_checks --env POSTGRES_USER=checks --env POSTGRES_PASSWORD=checks --tmpfs /var/lib/postgresql/data postgres:15
dotnet run --project backend/tests/KitchenRecipe.Checks -c Release
docker stop grimorio_kitchen_recipe_checks
```

Checks cover portion quantities, subrecipes, preparation, combo components,
missing data, branch boundaries, kitchen authorization and read-only behavior.
