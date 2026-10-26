# Part 8 endpoint contracts

The complete machine-readable contract is [Part8.EndpointContracts.json](Part8.EndpointContracts.json). The API tests compare it with all 101 approved Application requests, reflect controller metadata, and exercise each HTTP route. Request DTO definitions are in `src/Brouj.API/Requests` or the existing Application feature/shared DTO folders.

Every success response uses `ApiResponse<T>` (`success`, `data`, `message`). The response column below names `T`. JSON bodies containing unknown members are rejected. PATCH operations accept the full approved update DTO; they are not JSON Patch operations.

Application failures use `ApiErrorResponse` with the existing feature code/message: validation 400, unauthenticated 401, forbidden 403, missing resource 404 and business conflict 409. Unexpected failures and transport/model-binding failures use sanitized Problem Details. Existing optimistic concurrency exceptions become 409. Request/file limits return 413, unsupported content types 415, and endpoint throttling 429 with Retry-After. These mappings are transport guarantees; a particular handler only emits the errors defined by its approved use case. Tests inject each Result error category across every action to verify the common mapper, and separately exercise real handlers for authorization, ownership, validation, missing resources and conflicts.

All protected use cases still run `AuthorizationBehavior` / `FeatureAccess` to verify the current database account and role. Customer-specific handlers scope by trusted `ICurrentUser.CustomerId`; another customer's reservation is 404 for both GET and cancellation. Self profile/password commands never accept a customer/user ID. MemberOnly allows active customers and staff to read project time plans, as approved in Part 7. AdminOnly and StaffOnly allow Admin and SuperAdmin; staff management additionally requires SuperAdminOnly. The existing Infrastructure `CurrentUser` is reused rather than duplicating claim parsing.

All responses use `Cache-Control: no-store`, including public reads. This Part does not populate a feature/output cache. All safe reads use the PublicReads rate category, including protected reads; identity/session mutations use Authentication, customer reservation writes ReservationWrites, administrative mutations AdminWrites, and image uploads FileUploads. Limits are per category/client IP on each API instance.

Unsafe browser requests require a valid framework CSRF proof before authorization; missing/invalid proof is 400 even if the request would otherwise be 401/403. Protected request tests supply valid CSRF proof to isolate authorization outcomes. Public authentication routes remain anonymous at the route layer; refresh/logout handlers read only the appropriate secure refresh cookie.

`GET /api/v1/units/{id}`, `PATCH /api/v1/admin/units/{id}` and `DELETE /api/v1/admin/units/{id}` require `projectId` in the query string because their approved Application requests require both IDs. The route remains authoritative for the unit ID. Image child IDs and parent IDs are always assigned from route parameters.

JSON endpoints consume `application/json`; image creation consumes `multipart/form-data` with exactly one `File` and optional `AltText` (project/unit) or `Description` (stage item). The transport disposes its input stream; the existing storage adapter validates metadata, signatures, actual byte counts and generated paths. Individual files remain capped at 5 MiB; the separately configured multipart limit covers the entire envelope. List query DTOs retain the Application's defaults and maximum page size of 100.

| Method / route | MediatR request | JSON/form body; query DTO | Response data | Policy | Success | Rate category |
|---|---|---|---|---|---:|---|
| POST `/api/v1/auth/customers/register` | `RegisterCustomerCommand` | `RegisterCustomerInput`; — | `AuthSessionDto` | Anonymous | 201 | Authentication |
| POST `/api/v1/auth/customers/login` | `LoginCustomerCommand` | `LoginInput`; — | `AuthSessionDto` | Anonymous | 200 | Authentication |
| POST `/api/v1/auth/customers/refresh` | `RefreshCustomerSessionCommand` | —; — | `AuthSessionDto` | Anonymous | 200 | Authentication |
| POST `/api/v1/auth/customers/logout` | `LogoutCustomerCommand` | —; — | `Guid` | Anonymous | 200 | Authentication |
| POST `/api/v1/auth/users/login` | `LoginUserCommand` | `LoginInput`; — | `AuthSessionDto` | Anonymous | 200 | Authentication |
| POST `/api/v1/auth/users/refresh` | `RefreshUserSessionCommand` | —; — | `AuthSessionDto` | Anonymous | 200 | Authentication |
| POST `/api/v1/auth/users/logout` | `LogoutUserCommand` | —; — | `Guid` | Anonymous | 200 | Authentication |
| GET `/api/v1/customers/me` | `GetCurrentCustomerQuery` | —; — | `CurrentCustomerDto` | CustomerOnly | 200 | PublicReads |
| PATCH `/api/v1/customers/me` | `UpdateCustomerProfileCommand` | `CustomerProfileInput`; — | `Guid` | CustomerOnly | 200 | Authentication |
| POST `/api/v1/customers/me/change-password` | `ChangeCustomerPasswordCommand` | `PasswordChangeInput`; — | `Guid` | CustomerOnly | 200 | Authentication |
| GET `/api/v1/users/me` | `GetCurrentUserQuery` | —; — | `UserDto` | StaffOnly | 200 | PublicReads |
| POST `/api/v1/users/me/change-password` | `ChangeUserPasswordCommand` | `PasswordChangeInput`; — | `Guid` | StaffOnly | 200 | Authentication |
| GET `/api/v1/admin/users` | `GetUsersQuery` | —; `GetUsersParameters` | `PaginationResult<UserDto>` | SuperAdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/users/{id}` | `GetUserByIdQuery` | —; — | `UserDto` | SuperAdminOnly | 200 | PublicReads |
| POST `/api/v1/admin/users` | `CreateUserCommand` | `CreateUserInput`; — | `Guid` | SuperAdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/users/{id}` | `UpdateUserCommand` | `UpdateUserInput`; — | `Guid` | SuperAdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/users/{id}` | `SoftDeleteUserCommand` | —; — | `Guid` | SuperAdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/users/{id}/restore` | `RestoreUserCommand` | —; — | `Guid` | SuperAdminOnly | 200 | AdminWrites |
| GET `/api/v1/admin/customers` | `GetCustomersQuery` | —; `GetCustomersParameters` | `PaginationResult<CustomerDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/customers/{id}` | `GetCustomerByIdQuery` | —; — | `CustomerDto` | AdminOnly | 200 | PublicReads |
| DELETE `/api/v1/admin/customers/{id}` | `SoftDeleteCustomerCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/customers/{id}/restore` | `RestoreCustomerCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| GET `/api/v1/cities` | `GetCitiesQuery` | —; `GetCitiesParameters` | `PaginationResult<CityDto>` | Anonymous | 200 | PublicReads |
| GET `/api/v1/cities/{id}` | `GetCityByIdQuery` | —; — | `CityDto` | Anonymous | 200 | PublicReads |
| GET `/api/v1/cities/{cityId}/areas` | `GetAreasByCityQuery` | —; `GetAreasByCityParameters` | `PaginationResult<AreaListDto>` | Anonymous | 200 | PublicReads |
| GET `/api/v1/areas` | `GetAreasQuery` | —; `GetAreasParameters` | `PaginationResult<AreaListDto>` | Anonymous | 200 | PublicReads |
| GET `/api/v1/areas/{id}` | `GetAreaByIdQuery` | —; — | `AreaDto` | Anonymous | 200 | PublicReads |
| POST `/api/v1/admin/cities` | `CreateCityCommand` | `CityInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/cities/{id}` | `UpdateCityCommand` | `CityInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/cities/{id}` | `DeleteCityCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/areas` | `CreateAreaCommand` | `AreaInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/areas/{id}` | `UpdateAreaCommand` | `AreaInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/areas/{id}` | `DeleteAreaCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/areas/{id}/amenities` | `ReplaceAreaAmenitiesCommand` | `ReplaceAreaAmenitiesRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| GET `/api/v1/projects` | `GetProjectsQuery` | —; `GetProjectsParameters` | `PaginationResult<ProjectListDto>` | Anonymous | 200 | PublicReads |
| GET `/api/v1/projects/{id}` | `GetProjectByIdQuery` | —; — | `ProjectDto` | Anonymous | 200 | PublicReads |
| GET `/api/v1/projects/{projectId}/units` | `GetUnitsByProjectQuery` | —; `GetUnitsByProjectParameters` | `PaginationResult<UnitListDto>` | Anonymous | 200 | PublicReads |
| GET `/api/v1/projects/{projectId}/time-plan` | `GetProjectTimePlanQuery` | —; — | `TimePlanDto` | MemberOnly | 200 | PublicReads |
| POST `/api/v1/admin/projects` | `CreateProjectCommand` | `ProjectInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/projects/{id}` | `UpdateProjectCommand` | `ProjectInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/projects/{id}` | `DeleteProjectCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/projects/{id}/amenities` | `ReplaceProjectAmenitiesCommand` | `ReplaceProjectAmenitiesRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/projects/{id}/images` | `AddProjectImageCommand` | `AddProjectImageRequest`; — | `Guid` | AdminOnly | 201 | FileUploads |
| PATCH `/api/v1/admin/projects/{id}/images/{imageId}` | `UpdateProjectImageCommand` | `UpdateProjectImageRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/projects/{id}/images/{imageId}` | `DeleteProjectImageCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/projects/{projectId}/units` | `CreateUnitCommand` | `UnitInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| POST `/api/v1/admin/projects/{projectId}/time-plan` | `CreateTimePlanCommand` | `CreateTimePlanRequest`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| GET `/api/v1/units/{id}` | `GetUnitByIdQuery` | —; `GetUnitByIdParameters` | `UnitDto` | Anonymous | 200 | PublicReads |
| PATCH `/api/v1/admin/units/{id}` | `UpdateUnitCommand` | `UnitInput`; `UpdateUnitParameters` | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/units/{id}` | `DeleteUnitCommand` | —; `DeleteUnitParameters` | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/units/{id}/amenities` | `ReplaceUnitAmenitiesCommand` | `ReplaceUnitAmenitiesRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/units/{id}/images` | `AddUnitImageCommand` | `AddUnitImageRequest`; — | `Guid` | AdminOnly | 201 | FileUploads |
| PATCH `/api/v1/admin/units/{id}/images/{imageId}` | `UpdateUnitImageCommand` | `UpdateUnitImageRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/units/{id}/images/{imageId}` | `DeleteUnitImageCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/reservations` | `CreateReservationCommand` | `CreateReservationRequest`; — | `Guid` | CustomerOnly | 201 | ReservationWrites |
| GET `/api/v1/reservations/me` | `GetMyReservationsQuery` | —; `GetMyReservationsParameters` | `PaginationResult<ReservationDto>` | CustomerOnly | 200 | PublicReads |
| GET `/api/v1/reservations/{id}` | `GetReservationByIdQuery` | —; — | `ReservationDto` | CustomerOnly | 200 | PublicReads |
| POST `/api/v1/reservations/{id}/cancel` | `CancelReservationCommand` | —; — | `Guid` | CustomerOnly | 200 | ReservationWrites |
| GET `/api/v1/admin/reservations` | `GetReservationsQuery` | —; `GetReservationsParameters` | `PaginationResult<ReservationDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/reservations/{id}` | `GetReservationDetailsQuery` | —; — | `ReservationDto` | AdminOnly | 200 | PublicReads |
| POST `/api/v1/admin/reservations/{reservationId}/order` | `CreateOrderFromReservationCommand` | —; — | `Guid` | AdminOnly | 201 | AdminWrites |
| GET `/api/v1/orders/me` | `GetMyOrdersQuery` | —; `GetMyOrdersParameters` | `PaginationResult<OrderDto>` | CustomerOnly | 200 | PublicReads |
| GET `/api/v1/admin/orders` | `GetOrdersQuery` | —; `GetOrdersParameters` | `PaginationResult<OrderDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/orders/{id}` | `GetOrderByIdQuery` | —; — | `OrderDto` | MemberOnly | 200 | PublicReads |
| POST `/api/v1/admin/orders/{id}/complete` | `CompleteInitiativeOrderCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PATCH `/api/v1/admin/time-plans/{id}` | `UpdateTimePlanCommand` | `TimePlanInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/time-plans/{id}` | `DeleteTimePlanCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/time-plans/{timePlanId}/stages` | `AddTimePlanStageCommand` | `StageDraftInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| POST `/api/v1/admin/time-plans/{timePlanId}/stages/from-template` | `AddStageFromTemplateCommand` | `StageDraftInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/time-plans/{timePlanId}/stages/{id}` | `UpdateTimePlanStageCommand` | `TimePlanInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/time-plans/{timePlanId}/stages/{id}` | `DeleteTimePlanStageCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/time-plans/{timePlanId}/stages/order` | `ReorderTimePlanStagesCommand` | `ReorderTimePlanStagesRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/time-plan-stages/{stageId}/items` | `AddPlanStageItemCommand` | `PlanStageItemInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/time-plan-stages/{stageId}/items/{id}` | `UpdatePlanStageItemCommand` | `PlanStageItemInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/time-plan-stages/{stageId}/items/{id}` | `DeletePlanStageItemCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/time-plan-stages/{stageId}/items/{id}/status` | `ChangePlanStageItemStatusCommand` | `ChangePlanStageItemStatusRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/time-plan-stages/{stageId}/items/order` | `ReorderPlanStageItemsCommand` | `ReorderPlanStageItemsRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| POST `/api/v1/admin/time-plan-items/{itemId}/images` | `AddStageItemImageCommand` | `AddStageItemImageRequest`; — | `Guid` | AdminOnly | 201 | FileUploads |
| PATCH `/api/v1/admin/time-plan-items/{itemId}/images/{imageId}` | `UpdateStageItemImageCommand` | `UpdateStageItemImageRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/time-plan-items/{itemId}/images/{imageId}` | `DeleteStageItemImageCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/time-plan-items/{itemId}/images/order` | `ReorderStageItemImagesCommand` | `ReorderStageItemImagesRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| GET `/api/v1/admin/stage-templates` | `GetStageTemplatesQuery` | —; `GetStageTemplatesParameters` | `PaginationResult<StageTemplateListDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/stage-templates/{id}` | `GetStageTemplateByIdQuery` | —; — | `StageTemplateDto` | AdminOnly | 200 | PublicReads |
| POST `/api/v1/admin/stage-templates` | `CreateStageTemplateCommand` | `StageTemplateInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/stage-templates/{id}` | `UpdateStageTemplateCommand` | `StageTemplateInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/stage-templates/{id}/active` | `SetStageTemplateActiveCommand` | `SetStageTemplateActiveRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/stage-templates/{id}` | `DeleteStageTemplateCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/stage-templates/{id}/items` | `SetStageTemplateItemsCommand` | `SetStageTemplateItemsRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| GET `/api/v1/admin/item-templates` | `GetItemTemplatesQuery` | —; `GetItemTemplatesParameters` | `PaginationResult<ItemTemplateListDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/item-templates/{id}` | `GetItemTemplateByIdQuery` | —; — | `ItemTemplateDto` | AdminOnly | 200 | PublicReads |
| POST `/api/v1/admin/item-templates` | `CreateItemTemplateCommand` | `ItemTemplateInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| PATCH `/api/v1/admin/item-templates/{id}` | `UpdateItemTemplateCommand` | `ItemTemplateInput`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| PUT `/api/v1/admin/item-templates/{id}/active` | `SetItemTemplateActiveCommand` | `SetItemTemplateActiveRequest`; — | `Guid` | AdminOnly | 200 | AdminWrites |
| DELETE `/api/v1/admin/item-templates/{id}` | `DeleteItemTemplateCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| GET `/api/v1/terms-and-conditions/current` | `GetActiveTermsAndConditionsQuery` | —; — | `TermsDto` | Anonymous | 200 | PublicReads |
| GET `/api/v1/admin/terms-and-conditions` | `GetTermsAndConditionsVersionsQuery` | —; `GetTermsAndConditionsVersionsParameters` | `PaginationResult<TermsListDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/terms-and-conditions/{id}` | `GetTermsAndConditionsByIdQuery` | —; — | `TermsDto` | AdminOnly | 200 | PublicReads |
| POST `/api/v1/admin/terms-and-conditions` | `CreateTermsAndConditionsVersionCommand` | `CreateTermsVersionInput`; — | `Guid` | AdminOnly | 201 | AdminWrites |
| POST `/api/v1/admin/terms-and-conditions/{id}/activate` | `ActivateTermsAndConditionsCommand` | —; — | `Guid` | AdminOnly | 200 | AdminWrites |
| GET `/api/v1/admin/activity-logs` | `GetActivityLogsQuery` | —; `GetActivityLogsParameters` | `PaginationResult<ActivityLogDto>` | AdminOnly | 200 | PublicReads |
| GET `/api/v1/admin/activity-logs/{id}` | `GetActivityLogByIdQuery` | —; — | `ActivityLogDto` | AdminOnly | 200 | PublicReads |

## Supporting transport endpoints

| Method / route | Purpose and response | Policy | Success | Rate category | Cache |
|---|---|---|---:|---|---|
| GET `/api/v1/auth/csrf` | `ApiResponse<CsrfTokenResponse>` plus framework antiforgery cookie | Anonymous | 200 | Authentication | no-store |
| GET `/health/live` | Fixed `{"status":"Healthy"}`; no dependency, exception or configuration data | Anonymous | 200 | PublicReads | no-store |
| GET `/openapi/v1.json` | OpenAPI document, Development only; 404 in Production | Anonymous | 200 in Development | PublicReads | no-store |
| GET `/index.html` | Swagger UI, Development only; 404 in Production | Anonymous | 200 in Development | Development middleware; no endpoint policy | no-store |
| GET `/swagger/v1/swagger.json` | Swagger document, Development only; 404 in Production | Anonymous | 200 in Development | Development middleware; no endpoint policy | no-store |

These supporting endpoints do not represent business use cases and intentionally do not dispatch MediatR requests.
