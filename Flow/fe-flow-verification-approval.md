# Flow: Manager Reviews a Verification Request

> FE integration guide for the flow where a Manager reviews and approves/rejects a User's identity verification request.

---

## 1. Overview

A User submits an identity verification request (VerificationRequest) with document photos. The Manager reviews it and either:
- **Approve:** Confirms the user is a valid student
- **Reject:** Declines with a reason

Once approved, the user is granted the `verified=true` claim and can use features that require verification.

---

## 2. Sequence Diagram

```
User (FE)                    Manager (FE)                  Backend
    │                             │                            │
    │  Submit verification         │                            │
    │  POST /api/verification/     │                            │
    │  submit (multipart)          │                            │
    │─────────────────────────────>│                            │
    │                              │  Validate + upload to     │
    │                              │  Cloudinary + INSERT      │
    │                              │───────────────────────────>│
    │<─────────────────────────────│                            │
    │  201 Created                 │                            │
    │                              │                            │
    │                              │  1. List verifications     │
    │                              │  GET /api/admin/verif      │
    │                              │───────────────────────────>│
    │                              │<───────────────────────────│
    │                              │  Pending request list     │
    │                              │                            │
    │                              │  2. View details          │
    │                              │  GET /api/admin/verif/{id} │
    │                              │───────────────────────────>│
    │                              │<───────────────────────────│
    │                              │  Info + documents         │
    │                              │                            │
    │                              │  3a. Approve              │
    │                              │  POST .../approve         │
    │                              │───────────────────────────>│
    │                              │  Update User.EmailVerified│
    │                              │  + set claim              │
    │                              │<───────────────────────────│
    │                              │  200 OK                   │
    │                              │                            │
    │  (User receives notification)│                            │
    │<─────────────────────────────│                            │
    │                              │  3b. Reject               │
    │                              │  POST .../reject          │
    │                              │  { reason: "..." }        │
    │                              │───────────────────────────>│
    │                              │<───────────────────────────│
    │                              │  200 OK                   │
    │                              │                            │
    │  (User receives notification)│                            │
    │<─────────────────────────────│                            │
```

---

## 3. API Endpoints

### 3.1 List pending verifications

```
GET /api/admin/verifications
Auth: Manager
Query: ?pageNumber=1&pageSize=10
```

**Paginated response items:**
```json
{
  "items": [
    {
      "id": "guid",
      "userId": "guid",
      "userEmail": "student@example.com",
      "userName": "Nguyen Van A",
      "submittedAt": "2026-06-19T14:30:00Z",
      "documentCount": 2
    }
  ]
}
```

### 3.2 Get verification detail

```
GET /api/admin/verifications/{id}
Auth: Manager
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "userId": "guid",
    "userEmail": "student@example.com",
    "userName": "Nguyen Van A",
    "studentId": "SE123456",
    "majorOrClass": "Computer Science",
    "dateOfBirth": "2000-01-15",
    "status": 0,
    "submittedAt": "2026-06-19T14:30:00Z",
    "reviewedAt": null,
    "reviewedBy": null,
    "rejectionReason": null,
    "expiresAt": "2026-07-19T14:30:00Z",
    "documents": [
      {
        "id": "guid",
        "documentType": 0,
        "cloudinaryUrl": "https://res.cloudinary.com/...",
        "fileName": "student_card.jpg",
        "fileSize": 245000,
        "mimeType": "image/jpeg",
        "uploadedAt": "2026-06-19T14:30:00Z"
      }
    ]
  },
  "isSuccess": true
}
```

### 3.3 Approve

```
POST /api/admin/verifications/{id}/approve
Auth: Manager
Request body: None
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "message": "Verification request approved successfully."
  },
  "isSuccess": true
}
```

### 3.4 Reject

```
POST /api/admin/verifications/{id}/reject
Auth: Manager
```

**Request body:**
```json
{
  "reason": "The photo is unclear, please retake it."
}
```

**Response:**
```json
{
  "value": {
    "id": "guid",
    "message": "Verification request rejected."
  },
  "isSuccess": true
}
```

---

## 4. Suggested UI/UX

### List screen

```
┌─── ✅ Identity Verification Review ───────────────────────────────────┐
│                                                                        │
│  Filter: [All ▼]  Search: [______________]                            │
│                                                                        │
│  ┌──────┬──────────────┬──────────────────┬────────────┬──────────────┐│
│  │      │ User         │ Email            │ Submitted  │ Documents    ││
│  ├──────┼──────────────┼──────────────────┼────────────┼──────────────┤│
│  │ 🔴   │ Nguyen Van A │ sv1@example.com  │ 19/06/2026 │ 2 photos     ││
│  │ 🔴   │ Tran Thi B   │ sv2@example.com  │ 19/06/2026 │ 1 photo      ││
│  │ 🟢   │ Le Van C     │ sv3@example.com  │ 18/06/2026 │ 3 photos     ││
│  └──────┴──────────────┴──────────────────┴────────────┴──────────────┘│
│                                                          << < 1 > >>  │
│  🔴 = Pending        🟢 = Approved       ⚫ = Rejected                │
└────────────────────────────────────────────────────────────────────────┘
```

### Detail screen + actions

When clicking a pending item:

```
┌─── 📋 Verification Request Details ──────────────────────────────────┐
│                                                                       │
│  User info                                                            │
│  ─────────────────────────────────────────────────────────────        │
│  Full name:   Nguyen Van A                                            │
│  Email:       student@example.com                                     │
│  Student ID:  SE123456                                                │
│  Class:       Computer Science                                        │
│  Birth date:  15/01/2000                                              │
│  Submitted:   19/06/2026 14:30                                        │
│  Expires:     19/07/2026 14:30                                        │
│                                                                       │
│  Documents                                                            │
│  ─────────────────────────────────────────────────────────────        │
│  ┌────────────────────┐   ┌────────────────────┐                      │
│  │                    │   │                    │                      │
│  │  📷 Student card   │   │  📷 ID card        │                      │
│  │                    │   │                    │                      │
│  │  [View fullscreen] │   │  [View fullscreen] │                      │
│  └────────────────────┘   └────────────────────┘                      │
│                                                                       │
│  ─────────────────────────────────────────────────────────────        │
│                                                                       │
│  [✅ Approve]                  [❌ Reject]                            │
│                                                                       │
│  (Clicking Reject → show a modal to enter the reason)                 │
└───────────────────────────────────────────────────────────────────────┘
```

### Reject modal

```
┌─── Reject verification ──────────────────────────────┐
│                                                       │
│  Rejection reason:                                    │
│  ┌─────────────────────────────────────────────────┐ │
│  │ The photo is unclear, please retake it.         │ │
│  └─────────────────────────────────────────────────┘ │
│                                                       │
│  [Cancel]                          [Confirm reject]  │
└───────────────────────────────────────────────────────┘
```

### Approve confirmation modal

```
┌─── Confirm approval ──────────────────────────────────┐
│                                                       │
│  ✅ Approve the verification request of Nguyen Van A? │
│                                                       │
│  The user will be marked as "Verified"                │
│                                                       │
│  [Cancel]                          [Confirm approve] │
└───────────────────────────────────────────────────────┘
```

---

## 5. Document Types

| Value | Type | Description |
|-------|------|-------------|
| 0 | `StudentCard` | Student card |
| 1 | `Transcript` | Transcript |
| 2 | `Other` | Other documents |

---

## 6. Verification Status

| Value | Status | Color |
|-------|--------|-------|
| 0 | `Pending` | 🔴 Red (awaiting review) |
| 1 | `Approved` | 🟢 Green (approved) |
| 2 | `Rejected` | ⚫ Black (rejected) |
| 3 | `Expired` | ⚪ Gray (expired) |

---

## 7. Notification to the User

When the manager approves or rejects, the user receives a notification:

**Approve payload:**
```json
{
  "type": "verification_approved",
  "title": "Verification successful",
  "message": "Your identity verification request has been approved.",
  "referenceType": "VerificationRequest",
  "referenceId": "guid"
}
```

**Reject payload:**
```json
{
  "type": "verification_rejected",
  "title": "Verification rejected",
  "message": "Reason: The photo is unclear, please retake it.",
  "referenceType": "VerificationRequest",
  "referenceId": "guid"
}
```

---

## 8. FE integration checklist

| Step | Description | API |
|------|-------------|-----|
| 1 | Show a verification status badge/icon on the user profile | Data available in `GET /api/verification/me` |
| 2 | Manager lists all requests | `GET /api/admin/verifications` |
| 3 | Manager views details + document photos | `GET /api/admin/verifications/{id}` |
| 4 | Manager approves | `POST .../approve` |
| 5 | Manager rejects with a reason | `POST .../reject` |
| 6 | User receives the result notification | SignalR event `ReceiveNotification` |
