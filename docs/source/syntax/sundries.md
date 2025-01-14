# Sundries

## Inline text formatting

Note that there should be no space between the enclosing markers and the text.

**strong**, _emphasis_, `literal text`, \*escaped symbols\*

~~strikethrough~~ is supported through MyST `strikethrough` extension.

## Subscript & Superscript

H~2~O, and 4^th^ of July

## Block attributes

Using Myst's `attrs_block` extension, we can add attributes to a block-level element. For example we can use it to add a class to a header, paragraph, etc. Check out quotation section for another example.

## Quotation

Here is a quote. The attribution is added using the block attribute `attribution`.

{attribution="Hamlet act 4, Scene 5"}
> We know what we are, but know not what we may be.

## Line breaks

You can break a paragraph \
using `\` at the end of a line.

## Thematic break

Same as using `<hr>` HTML tag:
***

## Comments

% Here is a comment

You can use `%` to add comments in the markdown. Those comment will not be rendered in the HTML output. Look at the markdown source to see the comment above this paragraph.
