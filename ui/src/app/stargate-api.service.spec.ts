import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CreateDutyRequest, StargateApiService } from './stargate-api.service';

describe('StargateApiService', () => {
  let service: StargateApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        StargateApiService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(StargateApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets all people', () => {
    service.getPeople().subscribe(result => {
      expect(result.people.length).toBe(1);
    });

    const request = httpMock.expectOne('/api/Person');

    expect(request.request.method).toBe('GET');
    request.flush({
      success: true,
      message: '',
      responseCode: 200,
      people: [{ personId: 1, name: 'John Doe' }]
    });
  });

  it('creates a person as a JSON string body', () => {
    service.createPerson("Anne O'Neil").subscribe(result => {
      expect(result.id).toBe(3);
    });

    const request = httpMock.expectOne('/api/Person');

    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    expect(request.request.body).toBe(JSON.stringify("Anne O'Neil"));
    request.flush({
      success: true,
      message: '',
      responseCode: 200,
      id: 3
    });
  });

  it('encodes names in person and duty URLs', () => {
    service.getPerson('John Doe').subscribe();
    service.getDutiesByName('John Doe').subscribe();

    expect(httpMock.expectOne('/api/Person/John%20Doe').request.method).toBe('GET');
    expect(httpMock.expectOne('/api/AstronautDuty/John%20Doe').request.method).toBe('GET');
  });

  it('renames a person with newName in the body', () => {
    service.renamePerson('John Doe', 'Jane Doe').subscribe();

    const request = httpMock.expectOne('/api/Person/John%20Doe');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ newName: 'Jane Doe' });
    request.flush({
      success: true,
      message: '',
      responseCode: 200
    });
  });

  it('creates an astronaut duty', () => {
    const duty: CreateDutyRequest = {
      name: 'John Doe',
      rank: 'CAPT',
      dutyTitle: 'Pilot',
      dutyStartDate: '2026-01-01'
    };

    service.createDuty(duty).subscribe();

    const request = httpMock.expectOne('/api/AstronautDuty');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(duty);
    request.flush({
      success: true,
      message: '',
      responseCode: 200
    });
  });
});
