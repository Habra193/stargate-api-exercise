import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface BaseResponse {
  success: boolean;
  message: string;
  responseCode: number;
}

export interface PersonAstronaut {
  personId: number;
  name: string;
  currentRank?: string | null;
  currentDutyTitle?: string | null;
  careerStartDate?: string | null;
  careerEndDate?: string | null;
}

export interface AstronautDuty {
  id: number;
  personId: number;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate?: string | null;
}

export interface GetPeopleResult extends BaseResponse {
  people: PersonAstronaut[];
}

export interface GetPersonResult extends BaseResponse {
  person?: PersonAstronaut | null;
}

export interface GetDutiesResult extends BaseResponse {
  person?: PersonAstronaut | null;
  astronautDuties: AstronautDuty[];
}

export interface CreatePersonResult extends BaseResponse {
  id: number;
}

export interface CreateDutyRequest {
  name: string;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
}

@Injectable({ providedIn: 'root' })
export class StargateApiService {
  constructor(private readonly http: HttpClient) {}

  getPeople(): Observable<GetPeopleResult> {
    return this.http.get<GetPeopleResult>('/api/Person');
  }

  getPerson(name: string): Observable<GetPersonResult> {
    return this.http.get<GetPersonResult>(`/api/Person/${encodeURIComponent(name)}`);
  }

  createPerson(name: string): Observable<CreatePersonResult> {
    return this.http.post<CreatePersonResult>('/api/Person', JSON.stringify(name), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  renamePerson(oldName: string, newName: string): Observable<BaseResponse> {
    return this.http.put<BaseResponse>(`/api/Person/${encodeURIComponent(oldName)}`, { newName });
  }

  getDutiesByName(name: string): Observable<GetDutiesResult> {
    return this.http.get<GetDutiesResult>(`/api/AstronautDuty/${encodeURIComponent(name)}`);
  }

  createDuty(request: CreateDutyRequest): Observable<BaseResponse> {
    return this.http.post<BaseResponse>('/api/AstronautDuty', request);
  }
}
