# Kremetart — Baobab Ridge Wildlife Rehabilitation Records System

> **Origin name:** *Kremetart* is the Afrikaans word for the baobab — the "Tree of Life" that gives
> Baobab Ridge its name. Like the tree that stores water to survive the dry season, this system is
> the single dependable store that keeps every animal's recovery story alive until it is ready to
> return to the wild.

A C# Windows Forms desktop application (**.NET 8**) that records every animal in care at the
Baobab Ridge Wildlife Rehabilitation Centre, calculates its status and housing unit from the vet's
Recovery Score, and produces a summary report for the Centre manager. All data is stored in plain
text files (no database, no storage NuGet packages).

This project was built for the **PRG2782 Programming 2782** module (Project 2026).

---

## Team members (Team Kremetart)

| Name | Student number | Primary feature(s) |
|------|----------------|--------------------|
| _Member 1_ | _00000000_ |  |
| _Member 2_ | _00000000_ |  |
| _Member 3_ | _00000000_ |  |
| _Member 4_ | _00000000_ |  |
| _Member 5 | _00000000_ |  |

> Replace the placeholder rows above with the real identities.

---

## Features

All five required features are reachable from one clearly labelled window.

1. **Add a new animal** – labelled inputs for Animal ID, Name, Species, Age and Recovery Score.
   Every rule is validated before saving; the calculated status and housing unit are shown in the
   confirmation. A valid record is appended to `animals.txt` without touching existing lines.
2. **View all animals** – `animals.txt` is loaded at start-up into a read-only `DataGridView`
   with columns: Animal ID, Name, Species, Age, Recovery Score, Status, Housing Unit. The grid
   refreshes after every add, update and delete. Malformed lines are skipped and reported once.
3. **Update an animal** – load a record by clicking a grid row or by searching for its exact
   Animal ID. In edit mode the ID is read-only, Add is disabled and Save Changes is enabled. The
   status and housing unit are recalculated from the score and only the edited record changes.
4. **Delete an animal** – select a row and confirm ("Delete WR-0007 (Naledi)? This cannot be
   undone."). Only that record is removed; the grid refreshes and the form clears if needed.
5. **Generate a summary report** – totals, average age, average recovery score and per-status
   counts are shown on the form and written to `summary.txt` in the exact required layout.

### Classification rules (single method)

`AnimalClassifier.Classify` is the one place the bands are defined; both Add and Update call it.

| Recovery Score | Status | Housing Unit |
|----------------|--------|--------------|
| 0–19 | Critical | Intensive Care Unit |
| 20–39 | Serious | High-Dependency Ward |
| 40–59 | Stable | Recovery Ward |
| 60–79 | Recovering | Outdoor Enclosure |
| 80–100 | Release-Ready | Pre-Release Camp |

---

## How to build and run

### Requirements
- Windows with **Visual Studio 2022** and the **.NET 8 SDK** (or newer).
- The project targets `net8.0-windows` and uses Windows Forms.

### Steps
1. Clone the repository:
   ```
   git clone <repository-url>
   cd omendi-atlan
   ```
2. Open `BaobabRidge.sln` in Visual Studio.
3. Build and run with **F5** (or `dotnet run --project BaobabRidge/BaobabRidge.csproj`).

No configuration, connection strings or external services are needed. The application has no file
paths that are specific to one computer; `animals.txt` and `summary.txt` always live in the same
folder as the running `.exe`.

---

## Project structure

```
omendi-atlan/
├── BaobabRidge.sln
├── README.md
├── .gitignore
└── BaobabRidge/
    ├── BaobabRidge.csproj
    ├── Program.cs                 # entry point
    ├── MainForm.cs                # the window; hosts all five features
    ├── animals.txt                # starter data (copied next to the .exe)
    ├── Models/
    │   └── Animal.cs              # the Animal class
    └── Services/
        ├── AnimalClassifier.cs    # the single classification method
        ├── AnimalValidator.cs     # the validation rules (used by Add and Update)
        ├── AnimalRepository.cs    # load / append / rewrite animals.txt
        ├── SummaryService.cs      # summary calculation and summary.txt writer
        └── DataFileException.cs   # friendly, user-safe file error
```

---

## The two file formats

### `animals.txt`
- One animal per line, seven fields separated by the pipe character `|`, in this order:
  `ID | Name | Species | Age | Score | Status | Housing Unit`.
- No header line and no blank lines. UTF-8 encoding.
- Stored in the same folder as the running `.exe`. A starter file ships with the project
  (`Copy if newer`) so it appears beside the `.exe` after a build.
- When loading, any malformed line (wrong number of fields, non-numeric age or score, invalid ID)
  is skipped instead of crashing; the user is told once how many lines were skipped.
- Example:
  ```
  WR-0001|Thabo|African Penguin|4|85|Release-Ready|Pre-Release Camp
  WR-0002|Naledi|Serval|3|62|Recovering|Outdoor Enclosure
  WR-0004|Zola|Cape Vulture|12|19|Critical|Intensive Care Unit
  ```

### `summary.txt`
- Created beside the running `.exe` each time the summary is generated and **overwritten** each
  time (never appended).
- Timestamp format `yyyy-MM-dd HH:mm`; averages use **two decimals and a full stop**, regardless of
  the computer's regional settings; all five statuses are always listed, including zero counts.
- When there are no animals, counts are `0` and both averages show `N/A`.
- Example:
  ```
  BAOBAB RIDGE WILDLIFE REHABILITATION CENTRE
  ANIMAL SUMMARY REPORT
  Generated: 2026-10-14 09:32
  ----------------------------------------
  Total animals:            12
  Average age (years):      7.75
  Average recovery score:   54.00
  ----------------------------------------
  Animals per status:
    Critical:               2
    Serious:                2
    Stable:                 2
    Recovering:             3
    Release-Ready:          3
  ```
- `summary.txt` is generated at runtime and is **not** committed to Git.

---

## Error handling

All file operations (reading, appending, rewriting and writing the summary) are wrapped in error
handling. Missing files/folders, files locked by another program (`IOException`) and permission
problems (`UnauthorizedAccessException`) are caught and shown as a friendly message — never a stack
trace — and the program keeps running. A missing `animals.txt` is created empty and shows an empty
grid without an error.

To test: set `animals.txt` to read-only in its output folder, or open it in another program, then
try to add, update, delete or generate the summary.

---

## Git workflow

Each feature was developed on its own branch (for example `feature/add-animal`) and merged into the
stable `main` branch through a pull request. Commit messages start with a verb and describe the
change.


---

## Known limitations

- The application targets **Windows only** (Windows Forms); it does not run on macOS or Linux.
- Text fields cannot contain the pipe character `|` because it is the field separator; this is
  enforced by validation.
- The Animal ID cannot be changed after a record is saved (a requirement of the brief).
- The grid has no sorting or filtering beyond the exact-match Animal ID search.
- There is no undo for a delete; the confirmation dialog is the only safeguard.
