# Contributing Guidelines

You are more than welcome to contribute to `RawRabbit`. Here are some guidelines for the process.

## Create issue
With a few exceptions, every commits should be connected to an issue. That means that if you've found a bug or implemented a feature, it should be reported in the [issue section](https://github.com/pardahlman/RawRabbit/issues).

## Write code
Write as beautiful code as possible! `RawRabbit` is indented with [tabs and not spaces](http://ryanseddon.github.io/spaces-talk/images/batman-slap.jpg).

## Commit Code
Make sure that all commits start with `(#issue-number)`, like `(#19) Invoke message handlers in sync manner`. This way, the commits will appear in the issue and is easier found from the console `git log --grep #19`.

Follow the [official guide lines](https://www.git-scm.com/book/en/v2/Distributed-Git-Contributing-to-a-Project#Commit-Guidelines). In short, [the seven rules of a great git commit message](http://chris.beams.io/posts/git-commit/) should be honored:

1. Separate subject from body with a blank line
2. Limit the subject line to 50 characters
3. Capitalize the subject line
4. Do not end the subject line with a period
5. Use the imperative mood in the subject line
6. Wrap the body at 72 characters
7. Use the body to explain what and why vs. how

## Create Pull Request
Once the feature is developed, create a pull to `stable`.