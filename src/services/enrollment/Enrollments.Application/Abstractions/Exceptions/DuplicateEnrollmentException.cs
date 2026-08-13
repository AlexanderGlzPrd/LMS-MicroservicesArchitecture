namespace Enrollments.Application.Abstractions.Exceptions;

// La lanza el adaptador de persistencia al perder la carrera contra el indice unico.
// Nunca sale a HTTP: la captura EnrollStudentHandler y la traduce a "ya existia".
public sealed class DuplicateEnrollmentException(Exception innerException)
    : Exception("Ya existe una matricula para esa pareja de estudiante y curso.", innerException);
