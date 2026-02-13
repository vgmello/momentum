## Release 0.0.1

### New Rules

| Rule ID | Category                 | Severity | Notes                                                      |
|---------|--------------------------|----------|------------------------------------------------------------|
| MMT001  | DbCommandSourceGenerator | Warning  | NonQuery attribute used with generic ICommand<TResult>     |
| MMT002  | DbCommandSourceGenerator | Error    | Command missing ICommand<TResult> interface                |
| MMT003  | DbCommandSourceGenerator | Error    | Both Sp and Sql properties specified in DbCommandAttribute |
