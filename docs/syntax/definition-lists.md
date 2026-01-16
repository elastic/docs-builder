# Definition lists


A definition list in Markdown is created by writing a term on one line, followed by a colon and at least three spaces (`:   `) on the next line, followed by the definition. Multiple paragraphs in the definition should be indented with spaces to match the definition text.


## Basic definition list

::::{tab-set}

:::{tab-item} Output

Fruit
:   A sweet and fleshy product of a tree or other plant that contains seed and can be eaten as food. Common examples include apples, oranges, and bananas. Most fruits are rich in vitamins, minerals and fiber.

Vegetable
:   Any edible part of a plant that is used in savory dishes. This includes roots, stems, leaves, flowers, and sometimes fruits that are used as vegetables.

    Unlike fruits, vegetables are typically lower in sugar content and higher in fiber. Common examples include carrots, broccoli, spinach, potatoes, and tomatoes (which are technically fruits).

Grain
:   The edible seeds or fruit of grass-like plants belonging to the family Poaceae. These include wheat, rice, corn, oats, and barley. See [Types of Grains](https://en.wikipedia.org/wiki/Grain).

Legume
:   Plants in the family Fabaceae, or their fruit or seeds, such as peas, beans, lentils and peanuts. See [Common Legumes](https://en.wikipedia.org/wiki/Legume).

:::

:::{tab-item} Markdown

```markdown
Fruit
:   A sweet and fleshy product of a tree or other plant that contains seed and can be eaten as food. Common examples include apples, oranges, and bananas. Most fruits are rich in vitamins, minerals and fiber.

Vegetable
:   Any edible part of a plant that is used in savory dishes. This includes roots, stems, leaves, flowers, and sometimes fruits that are used as vegetables.

    Unlike fruits, vegetables are typically lower in sugar content and higher in fiber. Common examples include carrots, broccoli, spinach, potatoes, and tomatoes (which are technically fruits).

Grain
:   The edible seeds or fruit of grass-like plants belonging to the family Poaceae. These include wheat, rice, corn, oats, and barley. See [Types of Grains](https://en.wikipedia.org/wiki/Grain).

Legume
:   Plants in the family Fabaceae, or their fruit or seeds, such as peas, beans, lentils and peanuts. See [Common Legumes](https://en.wikipedia.org/wiki/Legume).
```

:::

::::


## Nested definition list

Definition lists can be also be nested by indenting the child definition list.

```markdown
Vegetable
:   Any edible part of a plant that is used in savory dishes. This includes roots, stems, leaves, flowers, and sometimes fruits that are used as vegetables.

Fruit
:   A sweet and fleshy product of a tree or other plant that contains seed and can be eaten as food. Common examples include apples, oranges, and bananas. Most fruits are rich in vitamins, minerals and fiber.
    
    Citrus
    :   Fruits with a leathery rind and segmented pulp, such as oranges, lemons, and grapefruits. High in vitamin C.  Common examples include oranges, lemons, and grapefruits.

    Stone fruit
    :   Fruits with a fleshy outer part surrounding a hard pit, such as peaches, plums, and cherries. Common examples include peaches, plums, and cherries.
```
