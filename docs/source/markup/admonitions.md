---
title: Admonitions
---

Admonitions bring the attention of readers.

## Basic admonitions

```{caution}
:name: caution_ref
This is a 'caution' admonition
```

```{note}
This is a 'note' admonition
```

```{tip}
This is a tip
```

## Version directives

```{versionadded} 0.3.2
Feature A
```

```{versionchanged} 8.15.0
Feature B
```

```{deprecated} 0.2.0
Feature C
```

## Nested admonitions

Admonitions like other directives can be nested.

````{note}
We can have nested admonitions.
```{tip}
Here is a tip.
```
````

## Collapsible admonitions

You can use `:class: dropdown` to make an admonition collapsible.

```{note}
:class: dropdown

Longer content can be collapsed to take less space.

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.
```


## Link to admonitions
You can add a 'name' option to an admonition, so that you can link to it elsewhere

Here is a [link to attention](#caution_ref)
