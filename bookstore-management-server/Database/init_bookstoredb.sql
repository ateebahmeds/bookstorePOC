-- Create database
CREATE DATABASE IF NOT EXISTS bookstoredb;

USE bookstoredb;

-- Create Authors table
CREATE TABLE IF NOT EXISTS Authors (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL
);

-- Create Books table
CREATE TABLE IF NOT EXISTS Books (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    ISBN VARCHAR(50) NOT NULL,
    AuthorId INT NOT NULL,
    Price DECIMAL(10, 2) NOT NULL,
    Stock INT NOT NULL,
    FOREIGN KEY (AuthorId) REFERENCES Authors(Id)
);

-- Create Users table
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL
);

-- Insert sample authors
INSERT INTO Authors (Name) VALUES ('Author One'), ('Author Two'), ('Author Three');

-- Insert sample books
INSERT INTO Books (Title, ISBN, AuthorId, Price, Stock) VALUES
('Book One', '1234567890', 1, 19.99, 10),
('Book Two', '0987654321', 2, 29.99, 5),
('Book Three', '1122334455', 3, 39.99, 2);

-- Insert sample user
INSERT INTO Users (Email, PasswordHash) VALUES ('testuser@example.com', 'AQAAAAEAACcQAAAAEExampleHashedPassword==');
