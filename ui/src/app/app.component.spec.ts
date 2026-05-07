import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AppComponent } from './app.component';
import { PersonAstronaut, StargateApiService } from './stargate-api.service';

describe('AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let component: AppComponent;
  let api: jasmine.SpyObj<StargateApiService>;

  const seededPeople: PersonAstronaut[] = [
    {
      personId: 1,
      name: 'John Doe',
      currentRank: '1LT',
      currentDutyTitle: 'Commander'
    },
    {
      personId: 2,
      name: 'Jane Doe',
      currentRank: null,
      currentDutyTitle: null
    }
  ];

  beforeEach(async () => {
    api = jasmine.createSpyObj<StargateApiService>('StargateApiService', [
      'getPeople',
      'createPerson',
      'renamePerson',
      'getDutiesByName',
      'createDuty'
    ]);

    api.getPeople.and.returnValue(of({
      success: true,
      message: '',
      responseCode: 200,
      people: seededPeople
    }));

    api.getDutiesByName.and.returnValue(of({
      success: true,
      message: '',
      responseCode: 200,
      person: seededPeople[0],
      astronautDuties: []
    }));

    spyOn(localStorage, 'getItem').and.returnValue(null);
    spyOn(localStorage, 'setItem');
    spyOn(window, 'matchMedia').and.returnValue({ matches: false } as MediaQueryList);

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [{ provide: StargateApiService, useValue: api }]
    }).compileComponents();

    fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });

  it('loads people on init and renders the application title', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(api.getPeople).toHaveBeenCalled();
    expect(component.people).toEqual(seededPeople);
    expect(compiled.querySelector('h1')?.textContent).toContain('Personnel Operations');
  });

  it('filters people by name', () => {
    fixture.detectChanges();

    component.personFilter = 'jane';

    expect(component.filteredPeople).toEqual([seededPeople[1]]);
  });

  it('loads duties and prepares forms when a person is selected', () => {
    fixture.detectChanges();

    component.selectPerson(seededPeople[0]);

    expect(component.selectedPerson).toEqual(seededPeople[0]);
    expect(component.renameTo).toBe('John Doe');
    expect(api.getDutiesByName).toHaveBeenCalledWith('John Doe');
  });

  it('rejects invalid person names before calling the API', () => {
    fixture.detectChanges();
    component.newPersonName = 'Bad Name!';

    component.addPerson();

    expect(api.createPerson).not.toHaveBeenCalled();
    expect(component.alertType).toBe('error');
    expect(component.alertMessage).toContain('apostrophes, and hyphens');
  });

  it('allows apostrophes and hyphens in person names', () => {
    fixture.detectChanges();
    api.createPerson.and.returnValue(of({
      success: true,
      message: '',
      responseCode: 200,
      id: 3
    }));
    component.newPersonName = "Anne O'Neil-Smith";

    component.addPerson();

    expect(api.createPerson).toHaveBeenCalledWith("Anne O'Neil-Smith");
    expect(component.alertMessage).toContain('was added');
  });

  it('rejects special characters in rank and duty title before calling the API', () => {
    fixture.detectChanges();
    component.selectPerson(seededPeople[0]);
    component.dutyForm.rank = 'CAPT!';
    component.dutyForm.dutyTitle = 'Pilot';

    component.addDuty();

    expect(api.createDuty).not.toHaveBeenCalled();
    expect(component.alertType).toBe('error');
    expect(component.alertMessage).toContain('Rank and duty title');
  });

  it('toggles and persists dark mode', () => {
    fixture.detectChanges();

    component.toggleTheme();

    expect(component.theme).toBe('dark');
    expect(localStorage.setItem).toHaveBeenCalledWith('stargate-theme', 'dark');
  });

  it('shows API errors from failed requests', () => {
    api.getPeople.and.returnValue(throwError(() => ({
      error: { message: 'API unavailable' }
    })));

    fixture.detectChanges();

    expect(component.alertType).toBe('error');
    expect(component.alertMessage).toBe('API unavailable');
  });
});
