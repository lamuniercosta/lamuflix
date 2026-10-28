# Library Collections

Vocabulary for the TMDb collection facet and movie membership in DEV-424. Terms were settled by Patron in CONCLUSIONS.md Q8.

## Language

**Collection**:
A named TMDb grouping of movies, identified by its positive TMDb collection id and carrying the returned name.
_Avoid_: Genre, generic programming collection.

**Collection membership**:
The optional association of one library movie with one Collection; a movie has zero or one membership.
_Avoid_: Multiple collection memberships, collection list.

**Collection facet**:
The library facet comprising nonempty Collections and their distinct current member counts.
_Avoid_: Grid grouping, counts narrowed by active filters.
