---
title: Extensions with Roles and Directives
---

It is easy to define new roles and directives in a project. Look at `docs\source\_ext\rejoin.py`. 
Some intro text here...

This extension turns "Alpha Beta Gamma" into {rejoin}`Alpha Beta Gamma` using the `rejoin` role.

And here we use the `rejoin` directive that gives us more configuration options:

```{rejoin} ID 
:from: ' '
:to: '___'
Alpha Beta Gamma
```
