# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.0] - 2026-10-02

### Added

- About dialog with the mdl-demo logo, the version, the project link, the
  license and the copyright
- Czech and German translations; the app uses the language of Windows, and
  About can switch to another one until the app is closed

### Changed

- The app is now `mdl-demo.exe` and the download is `mdl-demo-unsigned.zip`
- The project moved to https://github.com/mutms/mdl-demo-win
- Free disk space… moved from the main page to the About dialog
- Refresh is now an icon button next to the Demos heading
- Dialogs get narrower with the window instead of being cut off
- The source code moved from `win` to `src`

### Fixed

- The New demo dialog was cut off in a window at its smallest size
- The release zip could contain files left over from an earlier build

## [1.0.1] - 2026-09-30

### Added

- First version: create, start, stop and delete mdl-demo sites, open them in
  the browser and free disk space
