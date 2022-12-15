use core::slice;

use bulletproofs::{RangeProof, BulletproofGens, PedersenGens};
use curve25519_dalek_ng::{scalar::Scalar, ristretto::CompressedRistretto};
use merlin::Transcript;

#[no_mangle]
pub extern "C" fn bpgen_new(gens_capacity: u32, party_capacity: u32) -> *const BulletproofGens {
    let bp_gens = BulletproofGens::new(gens_capacity as usize, party_capacity as usize);
    Box::into_raw(Box::new(bp_gens))
}

#[no_mangle]
pub unsafe extern "C" fn bpgen_free(this: *mut BulletproofGens) {
    if this.is_null() {
        return;
    }
    drop(Box::from_raw(this));
}

#[repr(C)]
pub struct RangeProofWithCommit {
    pub proof: *mut RangeProof,
    pub point: *mut CompressedRistretto,
}


#[no_mangle]
pub unsafe extern "C" fn rangeproof_free(this: *mut RangeProof) {
    if this.is_null() {
        return;
    }
    drop(Box::from_raw(this));
}

#[no_mangle]
pub unsafe extern "C" fn rangeproof_prove_single(
    bp_gens: *const BulletproofGens,
    pc_gens: *const PedersenGens,
    v: u64,
    v_blinding: *const Scalar,
    n: u32,
    label: *const u8,
    label_len: i32,
) -> RangeProofWithCommit {
    let n = n as usize;
    let bp_gens = &*bp_gens;
    let pc_gens = &*pc_gens;
    let label = slice::from_raw_parts(label, label_len as usize);

    let v_blinding = &*v_blinding;

    let mut transcript = Transcript::new(label);
    let proof = RangeProof::prove_single(bp_gens, pc_gens, &mut transcript, v, v_blinding, n);
    let (proof, point) = proof.unwrap();

    RangeProofWithCommit {
        proof: Box::into_raw(Box::new(proof)),
        point: Box::into_raw(Box::new(point)),
    }
}

#[no_mangle]
pub unsafe extern "C" fn rangeproof_prove_multiple(
    bp_gens: *const BulletproofGens,
    pc_gens: *const PedersenGens,
    v: *const u64,
    v_blinding: *const Scalar, // this is a slice
    n: u32,
    label: *const u8,
    label_len: i32,
    amount: i32,
) -> RangeProofWithCommit {
    let n = n as usize;
    let bp_gens = &*bp_gens;
    let pc_gens = &*pc_gens;
    let label = slice::from_raw_parts(label, label_len as usize);

    let v_blinding = slice::from_raw_parts(v_blinding, amount as usize);
    let v = slice::from_raw_parts(v, amount as usize);

    let mut transcript = Transcript::new(label);
    let proof = RangeProof::prove_multiple(bp_gens, pc_gens, &mut transcript, v, v_blinding, n);

    let proof = proof.unwrap();
    let (proof, points) = proof;
    RangeProofWithCommit {
        proof: Box::into_raw(Box::new(proof)),
        point: points.as_ptr() as *mut _,
    }
}


#[no_mangle]
pub unsafe extern "C" fn rangeproof_verify_single(
    proof: *mut RangeProof,
    bp_gens: *const BulletproofGens,
    pc_gens: *const PedersenGens,
    commit: *const CompressedRistretto,
    n: u32,
    label: *const u8,
    label_len: i32,
) -> bool {
    let proof = &*proof;
    let n = n as usize;
    let bp_gens = &*bp_gens;
    let pc_gens = &*pc_gens;
    let commit = &*commit;
    let label = slice::from_raw_parts(label, label_len as usize);
    let mut transcript = Transcript::new(label);

    let res = proof.verify_single(bp_gens, pc_gens, &mut transcript, commit, n);

    res.is_ok()
}


#[no_mangle]
pub unsafe extern "C" fn rangeproof_verify_multiple(
    proof: *mut RangeProof,
    bp_gens: *const BulletproofGens,
    pc_gens: *const PedersenGens,
    commits: *const CompressedRistretto,
    n: u32,
    label: *const u8,
    label_len: i32,
    amount: u32,
) -> bool {
    let proof = &*proof;
    let n = n as usize;
    let bp_gens = &*bp_gens;
    let pc_gens = &*pc_gens;
    let commits = slice::from_raw_parts(commits, amount as usize);
    let label = slice::from_raw_parts(label, label_len as usize);
    let mut transcript = Transcript::new(label);

    let res = proof.verify_multiple(bp_gens, pc_gens, &mut transcript, commits, n);
    res.is_ok()
}
