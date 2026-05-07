import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AstronautDuty,
  CreateDutyRequest,
  PersonAstronaut,
  StargateApiService
} from './stargate-api.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  people: PersonAstronaut[] = [];
  duties: AstronautDuty[] = [];
  selectedPerson: PersonAstronaut | null = null;
  personFilter = '';
  newPersonName = '';
  renameTo = '';
  isBusy = false;
  alertMessage = '';
  alertType: 'info' | 'error' = 'info';
  theme: 'light' | 'dark' = 'light';
  readonly personNamePattern = "[A-Za-z0-9 '\\-]+";
  readonly plainTextPattern = '[A-Za-z0-9 ]+';
  dutyForm: CreateDutyRequest = this.emptyDutyForm();

  constructor(private readonly api: StargateApiService) {}

  ngOnInit(): void {
    this.loadTheme();
    this.refresh();
  }

  toggleTheme(): void {
    this.theme = this.theme === 'light' ? 'dark' : 'light';
    localStorage.setItem('stargate-theme', this.theme);
  }

  get filteredPeople(): PersonAstronaut[] {
    const filter = this.personFilter.trim().toLowerCase();
    if (!filter) {
      return this.people;
    }

    return this.people.filter(person => person.name.toLowerCase().includes(filter));
  }

  refresh(): void {
    this.run(() => {
      this.api.getPeople().subscribe({
        next: result => {
          this.people = result.people ?? [];
          if (this.selectedPerson) {
            const updatedPerson = this.people.find(person => person.personId === this.selectedPerson?.personId);
            this.selectedPerson = updatedPerson ?? null;
            this.renameTo = this.selectedPerson?.name ?? '';
          }
          this.finish();
        },
        error: error => this.fail(error)
      });
    });
  }

  selectPerson(person: PersonAstronaut): void {
    this.selectedPerson = person;
    this.renameTo = person.name;
    this.dutyForm = this.emptyDutyForm(person.name);
    this.loadDuties(person.name);
  }

  addPerson(): void {
    const name = this.newPersonName.trim();
    if (!name) {
      this.showError('Person name is required.');
      return;
    }

    if (!this.isValidPersonName(name)) {
      this.showError('Person name can only contain letters, numbers, spaces, apostrophes, and hyphens.');
      return;
    }

    this.run(() => {
      this.api.createPerson(name).subscribe({
        next: () => {
          this.newPersonName = '';
          this.showInfo(`${name} was added.`);
          this.refresh();
        },
        error: error => this.fail(error)
      });
    });
  }

  renameSelectedPerson(): void {
    if (!this.selectedPerson) {
      return;
    }

    const oldName = this.selectedPerson.name;
    const newName = this.renameTo.trim();
    if (!newName) {
      this.showError('New name is required.');
      return;
    }

    if (!this.isValidPersonName(newName)) {
      this.showError('Person name can only contain letters, numbers, spaces, apostrophes, and hyphens.');
      return;
    }

    this.run(() => {
      this.api.renamePerson(oldName, newName).subscribe({
        next: () => {
          this.showInfo(`${oldName} was renamed to ${newName}.`);
          this.refresh();
          this.loadDuties(newName);
        },
        error: error => this.fail(error)
      });
    });
  }

  addDuty(): void {
    if (!this.selectedPerson) {
      return;
    }

    const request: CreateDutyRequest = {
      name: this.selectedPerson.name,
      rank: this.dutyForm.rank.trim(),
      dutyTitle: this.dutyForm.dutyTitle.trim(),
      dutyStartDate: this.dutyForm.dutyStartDate
    };

    if (!request.rank || !request.dutyTitle || !request.dutyStartDate) {
      this.showError('Rank, duty title, and start date are required.');
      return;
    }

    if (!this.isValidPersonName(request.name)) {
      this.showError('Person name can only contain letters, numbers, spaces, apostrophes, and hyphens.');
      return;
    }

    if (!this.isPlainText(request.rank) || !this.isPlainText(request.dutyTitle)) {
      this.showError('Rank and duty title can only contain letters, numbers, and spaces.');
      return;
    }

    this.run(() => {
      this.api.createDuty(request).subscribe({
        next: () => {
          this.showInfo(`${request.dutyTitle} duty was added.`);
          this.dutyForm = this.emptyDutyForm(this.selectedPerson?.name);
          this.loadDuties(request.name);
          this.refresh();
        },
        error: error => this.fail(error)
      });
    });
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString();
  }

  private loadDuties(name: string): void {
    this.run(() => {
      this.api.getDutiesByName(name).subscribe({
        next: result => {
          this.selectedPerson = result.person ?? this.selectedPerson;
          this.duties = result.astronautDuties ?? [];
          this.finish();
        },
        error: error => this.fail(error)
      });
    });
  }

  private run(action: () => void): void {
    this.isBusy = true;
    action();
  }

  private finish(): void {
    this.isBusy = false;
  }

  private fail(error: { error?: { message?: string }; message?: string }): void {
    this.showError(error.error?.message || error.message || 'The API request failed.');
    this.finish();
  }

  private showInfo(message: string): void {
    this.alertType = 'info';
    this.alertMessage = message;
  }

  private showError(message: string): void {
    this.alertType = 'error';
    this.alertMessage = message;
  }

  private isValidPersonName(value: string): boolean {
    return /^[A-Za-z0-9 '-]+$/.test(value);
  }

  private isPlainText(value: string): boolean {
    return /^[A-Za-z0-9 ]+$/.test(value);
  }

  private emptyDutyForm(name = ''): CreateDutyRequest {
    return {
      name,
      rank: '',
      dutyTitle: '',
      dutyStartDate: new Date().toISOString().slice(0, 10)
    };
  }

  private loadTheme(): void {
    const savedTheme = localStorage.getItem('stargate-theme');
    if (savedTheme === 'dark' || savedTheme === 'light') {
      this.theme = savedTheme;
      return;
    }

    this.theme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
