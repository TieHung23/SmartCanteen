# Project Specification: SmartCanteen

## Overview

**SmartCanteen** is a specialized food management and ordering platform designed specifically for the **FPT University** ecosystem. The platform bridges the gap between canteen administrative planning and student dietary preferences by allowing structured yet customizable meal selection.

---

## 1. System Roles & Core Use Cases

### 1.1 Canteen Manager

The Manager acts as the administrator of the daily dining operations. Their primary responsibility is to define the "logic" of the meal service.

- **SessionMeal Configuration:**
  - Define the structure of a standard meal (e.g., a "Lunch Session" or "Dinner Session").
  - Set selection constraints: For example, a single plate must contain exactly **2 Meat types**, **1 Fish type**, and a choice of **Rice or Noodles**.
- **Daily Menu Management:**
  - Upload and categorize the specific dishes available for the day.
  - Assign dishes to their respective categories (Meat, Fish, Side, Base).
- **Operational Monitoring:**
  - Track the total volume of orders per session to assist the kitchen in portion control and waste reduction.

### 1.2 Student User

Students utilize the platform to ensure their meals are reserved and tailored to their preferences.

- **Meal Customization:**
  - View the available menu for the current session.
  - Build a personal meal plate by selecting items that satisfy the `SessionMeal` rules.
- **Order Execution:**
  - Finalize and pay for the customized plate for upcoming lunch or dinner windows.
- **History & Tracking:**
  - Review past meal configurations and dietary choices.

---

## 2. Business Rules & Logic

### 2.1 The Validation Engine

To maintain cost-effectiveness and nutritional balance, the system enforces the following:

- **Constraint Matching:** The "Add to Cart" or "Checkout" function is disabled unless the student's selection perfectly matches the `SessionMeal` requirements set by the manager.
- **Dynamic Inventory:** If a specific dish (e.g., Grilled Pork) runs out of stock, it is instantly hidden or disabled in the Student's customization view.

### 2.2 Time-Bound Ordering

- Managers can set cutoff times for sessions (e.g., Lunch orders must be finalized before 10:00 AM) to allow the kitchen staff to prepare the exact number of portions required.

---

## 3. Key Terminology

- **SessionMeal:** The template or "rule-set" defining the number of items allowed per category.
- **FoodList:** The collection of specific dishes prepared for a specific date.
- **CustomPlate:** The final selection made by a student, adhering to the session's constraints.

---

## 4. Proposed Technical Stack

- **Backend:** .NET 8/9 with Clean Architecture.
- **Database:** PostgreSQL for transactional integrity.
- **Architecture:** Microservices-ready to handle high-traffic spikes during peak campus meal hours.
