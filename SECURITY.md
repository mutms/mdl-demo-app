# Security

## Reporting a problem

Please do not report security problems in public issues. Report them
privately through GitHub instead:
[Report a vulnerability](https://github.com/mutms/mdl-demo-app/security/advisories/new).

Describe what you found, how to reproduce it and which version of MDL Demo
you used. You will get a reply as soon as the maintainer can look at it.

Only the latest release is supported; fixes go into a new release.

## What belongs elsewhere

- Problems in the demo sites themselves (the mdl-demo container image, its web
  console or the Moodle/MuTMS code in it) belong to
  [mdl-demo](https://github.com/mutms/mdl-demo).
- Problems in WSL or `wslc` belong to Microsoft:
  [microsoft/WSL](https://github.com/microsoft/WSL/security).

## What MDL Demo does

To help you judge a report, this is everything the app does on your computer:

- It runs `wslc` (WSL containers) with your normal user rights to list,
  create, start, stop and delete containers named `mdl-demo-NNNN`, and to
  download and remove the `ghcr.io/mutms/mdl-demo` image. It never asks for
  administrator rights.
- Demo ports are published on `127.0.0.1` only, so other computers on your
  network cannot reach a demo.
- It reads which ports other programs use, without opening any itself, and
  connects to `http://127.0.0.1:NNNN` to see when a new demo is ready.
- It opens demo addresses and Microsoft's WSL help pages in your default
  browser.

It stores no passwords or settings, and sends nothing anywhere; the only
download is the demo image, made by `wslc`.

## Unsigned releases

Release files are not code-signed yet, so Windows cannot confirm who made
them. Download MDL Demo only from this project's
[releases page](https://github.com/mutms/mdl-demo-app/releases), or build it
yourself from the source (see the [README](README.md#building)).
