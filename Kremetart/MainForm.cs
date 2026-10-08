using System.Globalization;
using Kremetart.Models;
using Kremetart.Services;

namespace Kremetart;

/// <summary>
/// The single window for the Kremetart — Baobab Ridge Wildlife Rehabilitation Records System.
/// It hosts all five required features: Add, View, Update, Delete and Summary.
/// The controls are built in code so the form is complete after a fresh clone.
/// </summary>
public class MainForm : Form
{
    // --- Input controls -------------------------------------------------
    private TextBox txtAnimalId = null!;
    private TextBox txtName = null!;
    private TextBox txtSpecies = null!;
    private TextBox txtAge = null!;
    private TextBox txtScore = null!;

    // --- Action buttons -------------------------------------------------
    private Button btnAdd = null!;
    private Button btnSaveChanges = null!;
    private Button btnClear = null!;
    private Button btnDelete = null!;
    private Button btnSearch = null!;
    private Button btnGenerateSummary = null!;

    // --- Search, grid and summary --------------------------------------
    private TextBox txtSearchId = null!;
    private DataGridView dgvAnimals = null!;
    private TextBox txtSummary = null!;

    // --- State ----------------------------------------------------------
    private readonly AnimalRepository _repository = AnimalRepository.InApplicationFolder();
    private readonly List<Animal> _animals = new();

    /// <summary>The ID of the record currently loaded for editing, or null in Add mode.</summary>
    private string? _editingId;

    /// <summary>Builds the form, its controls and loads the data file.</summary>
    public MainForm()
    {
        BuildInterface();
        LoadAnimalsFromFile();
    }

    // ===================================================================
    //  UI construction
    // ===================================================================

    /// <summary>
    /// Creates and lays out every control, then wires up the event handlers.
    /// </summary>
    private void BuildInterface()
    {
        Text = "Kremetart — Baobab Ridge Records System";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);
        Font = new Font("Segoe UI", 9F);

        // ---- Animal details group -------------------------------------
        var grpDetails = new GroupBox
        {
            Text = "Animal Details",
            Location = new Point(12, 12),
            Size = new Size(460, 230)
        };

        txtAnimalId = AddField(grpDetails, "Animal ID:", 30);
        txtName = AddField(grpDetails, "Name:", 60);
        txtSpecies = AddField(grpDetails, "Species:", 90);
        txtAge = AddField(grpDetails, "Age (years):", 120);
        txtScore = AddField(grpDetails, "Recovery Score:", 150);

        btnAdd = new Button { Text = "Add", Location = new Point(130, 185), Size = new Size(95, 30) };
        btnAdd.Click += BtnAdd_Click;

        btnSaveChanges = new Button
        {
            Text = "Save Changes",
            Location = new Point(235, 185),
            Size = new Size(110, 30),
            Enabled = false
        };
        btnSaveChanges.Click += BtnSaveChanges_Click;

        btnClear = new Button { Text = "Clear", Location = new Point(355, 185), Size = new Size(80, 30) };
        btnClear.Click += (_, _) => ResetToAddMode();

        grpDetails.Controls.AddRange(new Control[] { btnAdd, btnSaveChanges, btnClear });

        // ---- Find and delete group ------------------------------------
        var grpFind = new GroupBox
        {
            Text = "Find / Delete",
            Location = new Point(486, 12),
            Size = new Size(460, 120)
        };

        var lblSearch = new Label { Text = "Search by Animal ID:", Location = new Point(15, 32), AutoSize = true };
        txtSearchId = new TextBox { Location = new Point(150, 29), Width = 160 };
        txtSearchId.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) SearchForAnimal(); };

        btnSearch = new Button { Text = "Search", Location = new Point(320, 28), Size = new Size(90, 27) };
        btnSearch.Click += (_, _) => SearchForAnimal();

        btnDelete = new Button
        {
            Text = "Delete Selected",
            Location = new Point(150, 70),
            Size = new Size(160, 30),
            BackColor = Color.MistyRose
        };
        btnDelete.Click += BtnDelete_Click;

        grpFind.Controls.AddRange(new Control[] { lblSearch, txtSearchId, btnSearch, btnDelete });

        // ---- Summary group --------------------------------------------
        var grpSummary = new GroupBox
        {
            Text = "Summary Report",
            Location = new Point(486, 142),
            Size = new Size(460, 100)
        };

        btnGenerateSummary = new Button
        {
            Text = "Generate Summary",
            Location = new Point(15, 28),
            Size = new Size(150, 30)
        };
        btnGenerateSummary.Click += BtnGenerateSummary_Click;

        txtSummary = new TextBox
        {
            Location = new Point(15, 66),
            Width = 430,
            ReadOnly = true,
            TabStop = false,
            BackColor = SystemColors.Control,
            BorderStyle = BorderStyle.FixedSingle
        };

        grpSummary.Controls.AddRange(new Control[] { btnGenerateSummary, txtSummary });

        // ---- Grid ------------------------------------------------------
        dgvAnimals = new DataGridView
        {
            Location = new Point(12, 255),
            Size = new Size(934, 360),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window
        };
        dgvAnimals.Columns.Add("colId", "Animal ID");
        dgvAnimals.Columns.Add("colName", "Name");
        dgvAnimals.Columns.Add("colSpecies", "Species");
        dgvAnimals.Columns.Add("colAge", "Age");
        dgvAnimals.Columns.Add("colScore", "Recovery Score");
        dgvAnimals.Columns.Add("colStatus", "Status");
        dgvAnimals.Columns.Add("colHousing", "Housing Unit");
        dgvAnimals.CellClick += DgvAnimals_CellClick;

        Controls.AddRange(new Control[] { grpDetails, grpFind, grpSummary, dgvAnimals });
    }

    /// <summary>
    /// Adds a labelled text box to a parent group box and returns the text box.
    /// </summary>
    private static TextBox AddField(Control parent, string labelText, int top)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(15, top + 3),
            AutoSize = true
        };
        var box = new TextBox
        {
            Location = new Point(150, top),
            Width = 285
        };
        parent.Controls.AddRange(new Control[] { label, box });
        return box;
    }

    // ===================================================================
    //  Feature 2 - View (loading and refreshing the grid)
    // ===================================================================

    /// <summary>
    /// Loads animals.txt once at start-up, reports any skipped lines, and fills the grid.
    /// A missing file is created empty and simply shows an empty grid.
    /// </summary>
    private void LoadAnimalsFromFile()
    {
        try
        {
            List<Animal> loaded = _repository.LoadAll(out int skipped);
            _animals.Clear();
            _animals.AddRange(loaded);
            RefreshGrid();

            if (skipped > 0)
            {
                MessageBox.Show(this,
                    $"{skipped} line(s) in animals.txt were malformed and have been skipped.",
                    "Data file warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (DataFileException ex)
        {
            MessageBox.Show(this, ex.Message, "File error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Rebuilds the grid rows from the in-memory list, preserving file order and
    /// never producing duplicate rows.
    /// </summary>
    private void RefreshGrid()
    {
        dgvAnimals.Rows.Clear();
        foreach (Animal a in _animals)
        {
            dgvAnimals.Rows.Add(
                a.AnimalId,
                a.Name,
                a.Species,
                a.Age.ToString(CultureInfo.InvariantCulture),
                a.RecoveryScore.ToString(CultureInfo.InvariantCulture),
                a.Status,
                a.HousingUnit);
        }
    }

    // ===================================================================
    //  Feature 1 - Add
    // ===================================================================

    /// <summary>
    /// Validates the form, appends the new animal to animals.txt, shows the
    /// calculated status and housing unit, then refreshes and clears the form.
    /// </summary>
    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        ValidationResult result = AnimalValidator.Validate(
            txtAnimalId.Text, txtName.Text, txtSpecies.Text, txtAge.Text, txtScore.Text,
            _animals.Select(a => a.AnimalId));

        if (!result.IsValid)
        {
            ShowValidationError(result);
            return;
        }

        Animal animal = result.Animal!;

        try
        {
            _repository.Append(animal);
            _animals.Add(animal);
            RefreshGrid();

            MessageBox.Show(this,
                $"{animal.AnimalId} ({animal.Name}) was added.\n\n" +
                $"Status: {animal.Status}\nHousing Unit: {animal.HousingUnit}",
                "Animal added", MessageBoxButtons.OK, MessageBoxIcon.Information);

            ResetToAddMode();
        }
        catch (DataFileException ex)
        {
            MessageBox.Show(this, ex.Message, "File error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ===================================================================
    //  Feature 3 - Update
    // ===================================================================

    /// <summary>
    /// Loads the record of the clicked grid row into the form and switches to edit mode.
    /// </summary>
    private void DgvAnimals_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _animals.Count)
            return;

        LoadAnimalIntoForm(_animals[e.RowIndex]);
    }

    /// <summary>
    /// Finds a record by exact (case-insensitive) Animal ID and loads it for editing.
    /// If it is not found the user is told and nothing else changes.
    /// </summary>
    private void SearchForAnimal()
    {
        string id = txtSearchId.Text.Trim();
        if (id.Length == 0)
        {
            MessageBox.Show(this, "Type an Animal ID to search for.",
                "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Animal? match = _animals.FirstOrDefault(
            a => string.Equals(a.AnimalId, id, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            MessageBox.Show(this, $"No animal with ID {id.ToUpperInvariant()} was found.",
                "Not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        LoadAnimalIntoForm(match);
    }

    /// <summary>
    /// Fills the input boxes with an animal's values and enters edit mode:
    /// the Animal ID becomes read-only, Add is disabled and Save Changes is enabled.
    /// </summary>
    private void LoadAnimalIntoForm(Animal animal)
    {
        _editingId = animal.AnimalId;

        txtAnimalId.Text = animal.AnimalId;
        txtName.Text = animal.Name;
        txtSpecies.Text = animal.Species;
        txtAge.Text = animal.Age.ToString(CultureInfo.InvariantCulture);
        txtScore.Text = animal.RecoveryScore.ToString(CultureInfo.InvariantCulture);

        txtAnimalId.ReadOnly = true;
        btnAdd.Enabled = false;
        btnSaveChanges.Enabled = true;
        txtScore.Focus();
    }

    /// <summary>
    /// Saves the edited record. The same validation rules as Add are applied
    /// (uniqueness is skipped because the ID cannot change), the status and housing
    /// unit are recalculated from the score, and only the edited line is changed.
    /// </summary>
    private void BtnSaveChanges_Click(object? sender, EventArgs e)
    {
        if (_editingId == null)
            return;

        ValidationResult result = AnimalValidator.Validate(
            txtAnimalId.Text, txtName.Text, txtSpecies.Text, txtAge.Text, txtScore.Text);

        if (!result.IsValid)
        {
            ShowValidationError(result);
            return;
        }

        int index = _animals.FindIndex(
            a => string.Equals(a.AnimalId, _editingId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            MessageBox.Show(this, "The record could not be found. Reloading the data.",
                "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadAnimalsFromFile();
            ResetToAddMode();
            return;
        }

        // Replace only the edited record, keeping every other record as it was.
        _animals[index] = result.Animal!;

        try
        {
            _repository.SaveAll(_animals);
            RefreshGrid();
            MessageBox.Show(this,
                $"{result.Animal!.AnimalId} was updated.\n\n" +
                $"Status: {result.Animal.Status}\nHousing Unit: {result.Animal.HousingUnit}",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetToAddMode();
        }
        catch (DataFileException ex)
        {
            MessageBox.Show(this, ex.Message, "File error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Returns the form to Add mode: empty boxes, editable ID, Add enabled,
    /// Save Changes disabled, and no record loaded.
    /// </summary>
    private void ResetToAddMode()
    {
        _editingId = null;

        txtAnimalId.ReadOnly = false;
        txtAnimalId.Clear();
        txtName.Clear();
        txtSpecies.Clear();
        txtAge.Clear();
        txtScore.Clear();

        btnAdd.Enabled = true;
        btnSaveChanges.Enabled = false;

        dgvAnimals.ClearSelection();
        txtAnimalId.Focus();
    }

    // ===================================================================
    //  Feature 4 - Delete
    // ===================================================================

    /// <summary>
    /// Deletes the selected record after a Yes/No confirmation that shows the
    /// animal's ID and name. All other records are left untouched.
    /// </summary>
    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvAnimals.SelectedRows.Count == 0)
        {
            MessageBox.Show(this, "Please select a row in the grid to delete.",
                "Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int index = dgvAnimals.SelectedRows[0].Index;
        if (index < 0 || index >= _animals.Count)
            return;

        Animal target = _animals[index];

        DialogResult confirm = MessageBox.Show(this,
            $"Delete {target.AnimalId} ({target.Name})? This cannot be undone.",
            "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        _animals.RemoveAt(index);

        try
        {
            _repository.SaveAll(_animals);
            RefreshGrid();

            // If the deleted animal was loaded in the form, clear it.
            if (_editingId != null &&
                string.Equals(_editingId, target.AnimalId, StringComparison.OrdinalIgnoreCase))
            {
                ResetToAddMode();
            }

            MessageBox.Show(this, $"{target.AnimalId} ({target.Name}) was deleted.",
                "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (DataFileException ex)
        {
            // Put the record back so the in-memory list still matches the file.
            _animals.Insert(index, target);
            RefreshGrid();
            MessageBox.Show(this, ex.Message, "File error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ===================================================================
    //  Feature 5 - Summary report
    // ===================================================================

    /// <summary>
    /// Calculates totals, averages and per-status counts from animals.txt, shows
    /// them on the form and writes the same figures to summary.txt.
    /// </summary>
    private void BtnGenerateSummary_Click(object? sender, EventArgs e)
    {
        try
        {
            // Base the report on the data currently in the file.
            List<Animal> fromFile = _repository.LoadAll(out _);
            DateTime now = DateTime.Now;

            SummaryResult result = SummaryService.Calculate(fromFile);
            txtSummary.Text = BuildSummaryDisplay(result);
            string path = SummaryService.WriteReport(fromFile, now);

            MessageBox.Show(this, $"Summary report saved to:\n{path}",
                "Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (DataFileException ex)
        {
            MessageBox.Show(this, ex.Message, "File error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Formats the summary figures for the on-form read-only display.
    /// </summary>
    private static string BuildSummaryDisplay(SummaryResult result)
    {
        string age = result.HasAnimals
            ? result.AverageAge.ToString("F2", CultureInfo.InvariantCulture) : "N/A";
        string score = result.HasAnimals
            ? result.AverageRecoveryScore.ToString("F2", CultureInfo.InvariantCulture) : "N/A";

        var parts = new List<string>
        {
            $"Total: {result.TotalAnimals}",
            $"Avg age: {age}",
            $"Avg score: {score}"
        };

        foreach (string status in AnimalClassifier.AllStatuses)
        {
            int count = result.CountPerStatus.TryGetValue(status, out int c) ? c : 0;
            parts.Add($"{status}: {count}");
        }

        return string.Join("   |   ", parts);
    }

    // ===================================================================
    //  Validation feedback
    // ===================================================================

    /// <summary>
    /// Shows the validation message and puts the cursor back into the field
    /// that failed so the user can correct it.
    /// </summary>
    private void ShowValidationError(ValidationResult result)
    {
        MessageBox.Show(this, result.ErrorMessage, "Validation error",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);

        TextBox? field = result.FieldName switch
        {
            "Animal ID" => txtAnimalId,
            "Name" => txtName,
            "Species" => txtSpecies,
            "Age" => txtAge,
            "Recovery Score" => txtScore,
            _ => null
        };

        if (field != null)
        {
            field.Focus();
            field.SelectAll();
        }
    }
}
