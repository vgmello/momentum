## Release 0.0.1

### New Rules

| Rule ID           | Category                 | Severity | Notes                                                      |
|-------------------|--------------------------|----------|------------------------------------------------------------|
| DB_COMMAND_GEN001 | DbCommandSourceGenerator | Warning  | NonQuery attribute used with generic ICommand<TResult>     |
| DB_COMMAND_GEN002 | DbCommandSourceGenerator | Error    | Command missing ICommand<TResult> interface                |
| DB_COMMAND_GEN003 | DbCommandSourceGenerator | Error    | Both Sp and Sql properties specified in DbCommandAttribute |
