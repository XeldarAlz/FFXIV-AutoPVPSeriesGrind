## What

<!-- One or two sentences on what this PR changes. -->

## Why

<!-- The motivating problem, linked issue, or user-visible behavior this fixes. -->

Closes #

## How to test

<!--
Minimum steps a reviewer can run to verify the change. Testing usually means pressing Start with a small match limit and watching it queue, fight on the crystal, and leave at the results screen for at least one match end-to-end. Make sure the dependencies listed in /apsg deps are installed first. For UI-only changes, describe what to click.
-->

## Checklist

- [ ] `dotnet build -c Release` passes
- [ ] Verified in-game across at least one full match (queue -> fight -> leave -> requeue)
- [ ] If this changes user-visible behavior, README is updated
- [ ] If this touches the automation loop, relevant `[APSG]` log lines make the sequence auditable
